using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

[assembly: CommandClass(typeof(OhdDoorFinal.Commands))]
namespace OhdDoorFinal
{
   
    public class Commands
    {
        Editor ed = Application.DocumentManager.MdiActiveDocument.Editor;
        PromptPointOptions pointOptions;
        double[] flagPoint = new double[3] { -1000, -1000, -1000 };
        Point3d flaPoint;

        [CommandMethod("OHDDOOR")]
        public void door()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            pointOptions = new PromptPointOptions("");
            flaPoint = new Point3d(flagPoint);
            //Getting lower corner point
            Point3d gtPoint = GetPoint("Please select the inside corner point :");
            if (gtPoint == flaPoint) return;
            Point3d cornerPointInRev = gtPoint;

            //Getting upper corner point 
            gtPoint = GetPoint("Please select the any point on outside line :");
            if (gtPoint == flaPoint) return;
            Point3d cornerPointOutRev = gtPoint;

            PromptStringOptions stringPrompt = new PromptStringOptions("\nEnter the start distance from inside corner :");
            stringPrompt.AllowSpaces = false;

            //start distance
            PromptResult distanceString = ed.GetString(stringPrompt);
            if (distanceString.Status != PromptStatus.OK && decimal.TryParse(distanceString.StringResult, out _)) return;
            decimal startDistance;
            decimal.TryParse(distanceString.StringResult, out startDistance);

            //door width
            stringPrompt.Message = "\nEnter the door width :";
            distanceString = ed.GetString(stringPrompt);
            if (distanceString.Status != PromptStatus.OK && decimal.TryParse(distanceString.StringResult, out _)) return;
            decimal doorWidth;
            decimal.TryParse(distanceString.StringResult, out doorWidth);

            //Getting direction
            gtPoint = GetPoint("Select the direction to place the door");
            if (gtPoint == flaPoint) return;
            Point3d directionPoint = gtPoint;
            direction direction = GetDirection(cornerPointInRev, directionPoint);
            //Wall offset calc
            double wallOffset = Math.Abs(cornerPointOutRev.Y - cornerPointInRev.Y);
            ed.WriteMessage("\noffset distance :" + wallOffset);


            //Getting wall lines
            Line outsideLine = new Line();
            Line insideLine = new Line();


            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite) as BlockTableRecord;

                // Request for objects to be selected in the drawing area
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions();
                promptSelectionOptions.MessageForAdding = "\nSelect Outside && inside line and press enter";
                PromptSelectionResult SSPrompt = acDoc.Editor.GetSelection(promptSelectionOptions);

                //SelectionSet selectionSet = SSPrompt.Value;
                //Line outsideLine = new Line();
                //Line insideLine = new Line();
                if (SSPrompt.Status == PromptStatus.OK && SSPrompt.Value.Count == 2)
                {
                    foreach (ObjectId id in SSPrompt.Value.GetObjectIds())
                    {
                        if (id.ObjectClass.DxfName == "LINE")
                        {  
                            Line tempL = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            if (IsPointOnPolyline(ConvertToPolyline(tempL), cornerPointInRev))
                            {
                                insideLine = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            }
                            else if(IsPointOnPolyline(ConvertToPolyline(tempL), cornerPointOutRev))
                            {
                                outsideLine = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            }

                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nCancelled or selected more than two line");
                    return;
                }


                //Trim operation
                Point3d lineStartPoint = insideLine.StartPoint;
                Point3d lineEndPoint = insideLine.EndPoint;
                Polyline polyl = ConvertToPolyline(outsideLine);

                //Calculate trimming points 
                Point3d inTrimStart = new Point3d(), inTrimEnd = new Point3d(), outTrimStart = new Point3d(), outTrimEnd = new Point3d();
                switch (direction)
                {
                    case direction.right:
                        inTrimStart = new Point3d(cornerPointInRev.X + Convert.ToDouble(startDistance), cornerPointInRev.Y, 0);
                        inTrimEnd = new Point3d(inTrimStart.X + Convert.ToDouble(doorWidth), cornerPointInRev.Y, 0);
                        outTrimStart = new Point3d(inTrimStart.X, cornerPointOutRev.Y, 0);
                        outTrimEnd = new Point3d(inTrimEnd.X, cornerPointOutRev.Y, 0);
                        break;
                    case direction.left:
                        inTrimStart = new Point3d(cornerPointInRev.X - Convert.ToDouble(startDistance), cornerPointInRev.Y, 0);
                        inTrimEnd = new Point3d(inTrimStart.X - Convert.ToDouble(doorWidth), cornerPointInRev.Y, 0);
                        outTrimStart = new Point3d(inTrimStart.X, cornerPointOutRev.Y, 0);
                        outTrimEnd = new Point3d(inTrimEnd.X, cornerPointOutRev.Y, 0);
                        break;
                    case direction.top:
                        inTrimStart = new Point3d(cornerPointInRev.X, cornerPointInRev.Y + Convert.ToDouble(startDistance), 0);
                        inTrimEnd = new Point3d(cornerPointInRev.X, inTrimStart.Y + Convert.ToDouble(doorWidth), 0);
                        outTrimStart = new Point3d(cornerPointOutRev.X, inTrimStart.Y, 0);
                        outTrimEnd = new Point3d(cornerPointOutRev.X, inTrimEnd.Y, 0);
                        break;
                    case direction.bottom:
                        inTrimStart = new Point3d(cornerPointInRev.X, cornerPointInRev.Y - Convert.ToDouble(startDistance), 0);
                        inTrimEnd = new Point3d(cornerPointInRev.X, inTrimStart.Y - Convert.ToDouble(doorWidth), 0);
                        outTrimStart = new Point3d(cornerPointOutRev.X, inTrimStart.Y, 0);
                        outTrimEnd = new Point3d(cornerPointOutRev.X, inTrimEnd.Y, 0);
                        break;
                }


                Line tempLine = new Line(lineStartPoint, inTrimStart);
                if (IsPointOnPolyline(ConvertToPolyline(tempLine), inTrimEnd))
                {
                    Point3d tempPoint = lineStartPoint;
                    lineStartPoint = lineEndPoint;
                    lineEndPoint = tempPoint;
                }


                Line line1 = new Line(lineStartPoint, inTrimStart);
                Line line2 = new Line(inTrimEnd, lineEndPoint);

                line1.SetDatabaseDefaults();
                line2.SetDatabaseDefaults();
                polyl.SetDatabaseDefaults();

                acBlkTblRec.AppendEntity(line1);
                acBlkTblRec.AppendEntity(line2);
                acBlkTblRec.AppendEntity(polyl);

                tr.AddNewlyCreatedDBObject(line1, true);
                tr.AddNewlyCreatedDBObject(line2, true);
                tr.AddNewlyCreatedDBObject(polyl, true);

                //Upperline trim

                lineStartPoint = outsideLine.StartPoint;
                lineEndPoint = outsideLine.EndPoint;
                Polyline polyl2 = ConvertToPolyline(outsideLine);
                //Need to check trimStartPoint and trimEndPoint lies into the line or not
                if (!(IsPointOnPolyline(polyl2, outTrimStart) && IsPointOnPolyline(polyl2, outTrimEnd)))
                {
                    ed.WriteMessage("\nTrim startpoint and endpoint are not on the line to trim");
                    return;
                }

                tempLine = new Line(lineStartPoint, outTrimStart);
                if (IsPointOnPolyline(ConvertToPolyline(tempLine), outTrimEnd))
                {
                    Point3d tempPoint = lineStartPoint;
                    lineStartPoint = lineEndPoint;
                    lineEndPoint = tempPoint;
                }


                Line line3 = new Line(lineStartPoint, outTrimStart);
                Line line4 = new Line(outTrimEnd, lineEndPoint);

                line3.SetDatabaseDefaults();
                line4.SetDatabaseDefaults();
                polyl.SetDatabaseDefaults();
                polyl2.SetDatabaseDefaults();

                acBlkTblRec.AppendEntity(line3);
                acBlkTblRec.AppendEntity(line4);
                acBlkTblRec.AppendEntity(polyl2);

                tr.AddNewlyCreatedDBObject(line3, true);
                tr.AddNewlyCreatedDBObject(line4, true);
                tr.AddNewlyCreatedDBObject(polyl2, true);

                //Delete the base line to complete trimming operation
                polyl.Erase();
                polyl2.Erase();
                outsideLine.Erase();
                insideLine.Erase();

                //Add two more line connecting four trimming points
                Line startTrimConnection = new Line(inTrimStart, outTrimStart);
                Line endTrimConnection = new Line(inTrimEnd, outTrimEnd);
                startTrimConnection.SetDatabaseDefaults();
                endTrimConnection.SetDatabaseDefaults();
                acBlkTblRec.AppendEntity(startTrimConnection);
                acBlkTblRec.AppendEntity(endTrimConnection);
                tr.AddNewlyCreatedDBObject(startTrimConnection, true);
                tr.AddNewlyCreatedDBObject(endTrimConnection, true);


                ed.Regen();
                tr.Commit();


            }

            Point3d insertionPoint = GetPoint("Select door block insertion point");

            InsertBlock(insertionPoint, "door2", Convert.ToDouble(doorWidth));



        }


        [CommandMethod("OHDBLANK")]
        public void window()
        {
            // Get the current document and database
            Document acDoc = Application.DocumentManager.MdiActiveDocument;
            Database acCurDb = acDoc.Database;
            pointOptions = new PromptPointOptions("");
            flaPoint = new Point3d(flagPoint);
            //Getting lower corner point
            Point3d gtPoint = GetPoint("Please select the inside corner point :");
            if (gtPoint == flaPoint) return;
            Point3d cornerPointInRev = gtPoint;

            //Getting upper corner point 
            gtPoint = GetPoint("Please select the any point on outside line :");
            if (gtPoint == flaPoint) return;
            Point3d cornerPointOutRev = gtPoint;

            PromptStringOptions stringPrompt = new PromptStringOptions("\nEnter the start distance from inside corner :");
            stringPrompt.AllowSpaces = false;

            //start distance
            PromptResult distanceString = ed.GetString(stringPrompt);
            if (distanceString.Status != PromptStatus.OK && decimal.TryParse(distanceString.StringResult, out _)) return;
            decimal startDistance;
            decimal.TryParse(distanceString.StringResult, out startDistance);

            //door width
            stringPrompt.Message = "\nEnter the trimming width :";
            distanceString = ed.GetString(stringPrompt);
            if (distanceString.Status != PromptStatus.OK && decimal.TryParse(distanceString.StringResult, out _)) return;
            decimal windowWidth;
            decimal.TryParse(distanceString.StringResult, out windowWidth);

            //Getting direction
            gtPoint = GetPoint("Select the direction ");
            if (gtPoint == flaPoint) return;
            Point3d directionPoint = gtPoint;
            direction direction = GetDirection(cornerPointInRev, directionPoint);
            //Wall offset calc
            double wallOffset = Math.Abs(cornerPointOutRev.Y - cornerPointInRev.Y);
            ed.WriteMessage("\noffset distance :" + wallOffset);


            //Getting wall lines
            Line outsideLine = new Line();
            Line insideLine = new Line();


            using (Transaction tr = acCurDb.TransactionManager.StartTransaction())
            {
                // Open the Block table for read
                BlockTable acBlkTbl;
                acBlkTbl = tr.GetObject(acCurDb.BlockTableId,
                OpenMode.ForRead) as BlockTable;
                // Open the Block table record Model space for write
                BlockTableRecord acBlkTblRec;
                acBlkTblRec = tr.GetObject(acBlkTbl[BlockTableRecord.ModelSpace],
                OpenMode.ForWrite) as BlockTableRecord;

                // Request for objects to be selected in the drawing area
                PromptSelectionOptions promptSelectionOptions = new PromptSelectionOptions();
                promptSelectionOptions.MessageForAdding = "\nSelect Outside && inside line and press enter";
                PromptSelectionResult SSPrompt = acDoc.Editor.GetSelection(promptSelectionOptions);

                //SelectionSet selectionSet = SSPrompt.Value;
                //Line outsideLine = new Line();
                //Line insideLine = new Line();
                if (SSPrompt.Status == PromptStatus.OK && SSPrompt.Value.Count == 2)
                {
                    foreach (ObjectId id in SSPrompt.Value.GetObjectIds())
                    {
                        if (id.ObjectClass.DxfName == "LINE")
                        {
                            Line tempL = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            if (IsPointOnPolyline(ConvertToPolyline(tempL), cornerPointInRev))
                            {
                                insideLine = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            }
                            else if (IsPointOnPolyline(ConvertToPolyline(tempL), cornerPointOutRev))
                            {
                                outsideLine = tr.GetObject(id, OpenMode.ForWrite) as Line;
                            }

                        }
                    }
                }
                else
                {
                    ed.WriteMessage("\nCancelled or selected more than two line");
                    return;
                }


                //Trim operation
                Point3d lineStartPoint = insideLine.StartPoint;
                Point3d lineEndPoint = insideLine.EndPoint;
                Polyline polyl = ConvertToPolyline(outsideLine);

                //Calculate trimming points 
                Point3d inTrimStart = new Point3d(), inTrimEnd = new Point3d(), outTrimStart = new Point3d(), outTrimEnd = new Point3d();
                switch (direction)
                {
                    case direction.right:
                        inTrimStart = new Point3d(cornerPointInRev.X + Convert.ToDouble(startDistance), cornerPointInRev.Y, 0);
                        inTrimEnd = new Point3d(inTrimStart.X + Convert.ToDouble(windowWidth), cornerPointInRev.Y, 0);
                        outTrimStart = new Point3d(inTrimStart.X, cornerPointOutRev.Y, 0);
                        outTrimEnd = new Point3d(inTrimEnd.X, cornerPointOutRev.Y, 0);
                        break;
                    case direction.left:
                        inTrimStart = new Point3d(cornerPointInRev.X - Convert.ToDouble(startDistance), cornerPointInRev.Y, 0);
                        inTrimEnd = new Point3d(inTrimStart.X - Convert.ToDouble(windowWidth), cornerPointInRev.Y, 0);
                        outTrimStart = new Point3d(inTrimStart.X, cornerPointOutRev.Y, 0);
                        outTrimEnd = new Point3d(inTrimEnd.X, cornerPointOutRev.Y, 0);
                        break;
                    case direction.top:
                        inTrimStart = new Point3d(cornerPointInRev.X, cornerPointInRev.Y + Convert.ToDouble(startDistance), 0);
                        inTrimEnd = new Point3d(cornerPointInRev.X, inTrimStart.Y + Convert.ToDouble(windowWidth), 0);
                        outTrimStart = new Point3d(cornerPointOutRev.X, inTrimStart.Y, 0);
                        outTrimEnd = new Point3d(cornerPointOutRev.X, inTrimEnd.Y, 0);
                        break;
                    case direction.bottom:
                        inTrimStart = new Point3d(cornerPointInRev.X, cornerPointInRev.Y - Convert.ToDouble(startDistance), 0);
                        inTrimEnd = new Point3d(cornerPointInRev.X, inTrimStart.Y - Convert.ToDouble(windowWidth), 0);
                        outTrimStart = new Point3d(cornerPointOutRev.X, inTrimStart.Y, 0);
                        outTrimEnd = new Point3d(cornerPointOutRev.X, inTrimEnd.Y, 0);
                        break;
                }


                Line tempLine = new Line(lineStartPoint, inTrimStart);
                if (IsPointOnPolyline(ConvertToPolyline(tempLine), inTrimEnd))
                {
                    Point3d tempPoint = lineStartPoint;
                    lineStartPoint = lineEndPoint;
                    lineEndPoint = tempPoint;
                }


                Line line1 = new Line(lineStartPoint, inTrimStart);
                Line line2 = new Line(inTrimEnd, lineEndPoint);

                line1.SetDatabaseDefaults();
                line2.SetDatabaseDefaults();
                polyl.SetDatabaseDefaults();

                acBlkTblRec.AppendEntity(line1);
                acBlkTblRec.AppendEntity(line2);
                acBlkTblRec.AppendEntity(polyl);

                tr.AddNewlyCreatedDBObject(line1, true);
                tr.AddNewlyCreatedDBObject(line2, true);
                tr.AddNewlyCreatedDBObject(polyl, true);

                //Upperline trim

                lineStartPoint = outsideLine.StartPoint;
                lineEndPoint = outsideLine.EndPoint;
                Polyline polyl2 = ConvertToPolyline(outsideLine);
                //Need to check trimStartPoint and trimEndPoint lies into the line or not
                if (!(IsPointOnPolyline(polyl2, outTrimStart) && IsPointOnPolyline(polyl2, outTrimEnd)))
                {
                    ed.WriteMessage("\nTrim startpoint and endpoint are not on the line to trim");
                    return;
                }

                tempLine = new Line(lineStartPoint, outTrimStart);
                if (IsPointOnPolyline(ConvertToPolyline(tempLine), outTrimEnd))
                {
                    Point3d tempPoint = lineStartPoint;
                    lineStartPoint = lineEndPoint;
                    lineEndPoint = tempPoint;
                }


                Line line3 = new Line(lineStartPoint, outTrimStart);
                Line line4 = new Line(outTrimEnd, lineEndPoint);

                line3.SetDatabaseDefaults();
                line4.SetDatabaseDefaults();
                polyl.SetDatabaseDefaults();
                polyl2.SetDatabaseDefaults();

                acBlkTblRec.AppendEntity(line3);
                acBlkTblRec.AppendEntity(line4);
                acBlkTblRec.AppendEntity(polyl2);

                tr.AddNewlyCreatedDBObject(line3, true);
                tr.AddNewlyCreatedDBObject(line4, true);
                tr.AddNewlyCreatedDBObject(polyl2, true);

                //Delete the base line to complete trimming operation
                polyl.Erase();
                polyl2.Erase();
                outsideLine.Erase();
                insideLine.Erase();

                //Add two more line connecting four trimming points
                Line startTrimConnection = new Line(inTrimStart, outTrimStart);
                Line endTrimConnection = new Line(inTrimEnd, outTrimEnd);
                startTrimConnection.SetDatabaseDefaults();
                endTrimConnection.SetDatabaseDefaults();
                acBlkTblRec.AppendEntity(startTrimConnection);
                acBlkTblRec.AppendEntity(endTrimConnection);
                tr.AddNewlyCreatedDBObject(startTrimConnection, true);
                tr.AddNewlyCreatedDBObject(endTrimConnection, true);


                ed.Regen();
                tr.Commit();


            }

            //Point3d insertionPoint = GetPoint("Select window block insertion point");

            //Point3d modifiedPos = new Point3d(insertionPoint.X + 905, insertionPoint.Y - 544, 0);
            
            //InsertBlock(modifiedPos, "OHD Window");
        }

        /// <summary>
        /// This methods returns distance between two points
        /// </summary>
        /// <param name="point1"></param>
        /// <param name="point2"></param>
        /// <returns></returns>
        public double GetDistBetPoints(Point3d point1, Point3d point2)
        {
            return Math.Sqrt(Math.Pow((point1.X - point2.X), 2) + Math.Pow((point1.Y - point2.Y), 2));
        }

        /// <summary>
        /// This method prompt to get point from autocad.Do not need to enter new line string in message.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public Point3d GetPoint(string message)
        {
            //Getting lower corner point
            pointOptions.Message = "\n" + message;
            pointOptions.AllowArbitraryInput = false;
            pointOptions.AllowNone = true;
            PromptPointResult prPtRes1 = ed.GetPoint(pointOptions);
            if (prPtRes1.Status != PromptStatus.OK) return flaPoint;
            return prPtRes1.Value;
        }

        /// <summary>
        /// This method will return direction of trim
        /// </summary>
        /// <param name="cornerPointLower"></param>
        /// <param name="directionPoint"></param>
        /// <returns></returns>
        public direction GetDirection(Point3d cornerPointLower, Point3d directionPoint)
        {
            //Calculating direction
            direction direction;
            double xFactor, yFactor;
            xFactor = directionPoint.X - cornerPointLower.X;
            yFactor = directionPoint.Y - cornerPointLower.Y;

            if (Math.Abs(xFactor) > Math.Abs(yFactor)) //X is the determinent direction: right or left
            {
                if (xFactor > 0)
                {
                    direction = direction.right;
                }
                else
                {
                    direction = direction.left;
                }

            }
            else //Y is the determinent direction: Top or bottom
            {
                if (yFactor > 0)
                {
                    direction = direction.top;
                }
                else
                {
                    direction = direction.bottom;
                }
            }
            return direction;
        }

        /// <summary>
        /// This method will trim a line from a start point to end point
        /// This method can trim only horizontal or vertical line
        /// </summary>
        /// <param name="lineToTrim"></param>
        /// <param name="trimStartPoint"></param>
        /// <param name="trimEndPoint"></param>
        public void trimLine(Line lineToTrim, Point3d trimStartPoint, Point3d trimEndPoint)
        {

        }

        /// <summary>
        /// Insert a block to modelspace
        /// </summary>
        /// <param name="insPt"></param>
        /// <param name="blockName"></param>
        public void InsertBlock(Point3d insPt, string blockName, double width = 0)
        {
            Document doc = Application.DocumentManager.MdiActiveDocument;

            using (Database OpenDb = new Database(false, true))

            {

                OpenDb.ReadDwgFile(@"C:\temp\Blocks\blocks.dwg",

                    System.IO.FileShare.ReadWrite, true, "");



                ObjectIdCollection ids = new ObjectIdCollection();

                using (Transaction tr =

                        OpenDb.TransactionManager.StartTransaction())

                {

                    //For example, Get the block by name "TEST"

                    BlockTable bt;

                    bt = (BlockTable)tr.GetObject(OpenDb.BlockTableId

                                                   , OpenMode.ForRead);



                    if (bt.Has(blockName))

                    {

                        ids.Add(bt[blockName]);

                    }

                    tr.Commit();

                }



                //if found, add the block

                if (ids.Count != 0)

                {

                    //get the current drawing database

                    Database destdb = doc.Database;



                    IdMapping iMap = new IdMapping();

                    destdb.WblockCloneObjects(ids, destdb.BlockTableId

                           , iMap, DuplicateRecordCloning.Ignore, false);

                }

                InsertBlockToPoint(insPt, blockName, width);
            }


        }

        /// <summary>
        /// Insert a block to the point
        /// </summary>
        /// <param name="insPt"></param>
        /// <param name="blockName"></param>
        public void InsertBlockToPoint(Point3d insPt, string blockName, double width = 0)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;
            using (var tr = db.TransactionManager.StartTransaction())
            {
                // check if the block table already has the 'blockName'" block
                var bt = (BlockTable)tr.GetObject(db.BlockTableId, OpenMode.ForRead);
                if (!bt.Has(blockName))
                {
                    try
                    {
                        // search for a dwg file named 'blockName' in AutoCAD search paths
                        var filename = HostApplicationServices.Current.FindFile(blockName + ".dwg", db, FindFileHint.Default);
                        // add the dwg model space as 'blockName' block definition in the current database block table
                        using (var sourceDb = new Database(false, true))
                        {
                            sourceDb.ReadDwgFile(filename, FileOpenMode.OpenForReadAndAllShare, true, "");
                            db.Insert(blockName, sourceDb, true);
                        }
                    }
                    catch
                    {
                        ed.WriteMessage($"\nBlock '{blockName}' not found.");
                        return;
                    }
                }

                // create a new block reference
                using (var br = new BlockReference(insPt, bt[blockName]))
                {
                    var space = (BlockTableRecord)tr.GetObject(db.CurrentSpaceId, OpenMode.ForWrite);
                    space.AppendEntity(br);
                    tr.AddNewlyCreatedDBObject(br, true);

                    if (br.IsDynamicBlock)
                    {
                        var dynProps = br.DynamicBlockReferencePropertyCollection;
                        foreach (DynamicBlockReferenceProperty dynProp in dynProps)
                        {
                            if (!dynProp.ReadOnly && Regex.IsMatch(dynProp.PropertyName, "Distance1", RegexOptions.IgnoreCase))
                            {

                                if (dynProp.PropertyName == "Distance1")
                                {
                                    if (width != 0)
                                        dynProp.Value = width;

                                }
                            }
                        }
                    }
                }
                tr.Commit();
            }
        }

        /// Pass a database-resident line or arc to this method, and it will 
        /// return a new Polyline, which you must add to a database or dispose.

        public static Polyline ConvertToPolyline(Curve curve)
        {
            if (!(curve is Line || curve is Arc))
                throw new ArgumentException("requires a Line or Arc");
            Polyline pline = new Polyline();
            pline.TransformBy(curve.Ecs);
            Point3d start = curve.StartPoint.TransformBy(pline.Ecs.Inverse());
            pline.AddVertexAt(0, new Point2d(start.X, start.Y), 0, 0, 0);
            pline.JoinEntity(curve);
            return pline;
        }

        /// <summary>
        /// This method will check a point on a polyline or not
        /// </summary>
        /// <param name="pl"></param>
        /// <param name="pt"></param>
        /// <returns></returns>
        private bool IsPointOnPolyline(Polyline pl, Point3d pt)

        {

            bool isOn = false;

            for (int i = 0; i < pl.NumberOfVertices; i++)

            {

                Curve3d seg = null;



                SegmentType segType = pl.GetSegmentType(i);

                if (segType == SegmentType.Arc)

                    seg = pl.GetArcSegmentAt(i);

                else if (segType == SegmentType.Line)

                    seg = pl.GetLineSegmentAt(i);



                if (seg != null)

                {

                    isOn = seg.IsOn(pt);

                    if (isOn)

                        break;

                }

            }

            return isOn;

        }
    }

    public enum direction
    {
        right,
        left,
        top,
        bottom
    }
}
