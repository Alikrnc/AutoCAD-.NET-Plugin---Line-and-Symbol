﻿using Autodesk.AutoCAD.Runtime;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Colors;

using AcGi = Autodesk.AutoCAD.GraphicsInterface;

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using WinForms = System.Windows.Forms;
using Autodesk.AutoCAD.Windows;
using System.Windows.Forms;
using System.Text;

namespace Line_and_Symbol
{
    [AttributeUsage(AttributeTargets.Method)]
    public class PaletteMethod : Attribute { }
    
    public class LineSymbol       
        : Autodesk.AutoCAD.Runtime.IExtensionApplication
        // In order to use Initialize and Terminate functions we should call
        // Runtime.IExtensionApplication statement but when we call this
        // statement we must use both of Initialize and Terminate Methods.
    {
        const double pi = Math.PI; // Value of pi to reach it easily

        public void Initialize()
        {
            /*
             * Initialize method will be called when the dll file
             * is loaded on AutoCAD with "netload" command.
             
             * In this project we are using Initialize method
             * in order to add the electrical symbols to the
             * AutoCAD database.
            */

            LSLoad(); // In this method we have the other symbols adding methods.          
        }

        public void Terminate()
        {
            // It is a method that will be called
            // when the program is terminated.

            // Must be used when the Runtime.IExtensionApplication 
            // statement is used in project.

            Console.WriteLine("Terminating...");
        }
        
        // This line lets the user call any command in AutoCAD
        // it can be written as [CommandMethod("CommandName")]
        // just before the method.
        [CommandMethod("LSLoad")] 
        public void LSLoad()
        {
            // In this method, we have called the methods that
            // create symbols and add them to the AutoCAD database.

            /*
             * To access Document element we can use:             
             * Application.DocumentManager.MdiActiveDocument; line but in here we have used it with             
             * Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;             
             * because we also have WinForms.Application and it causes errors because of ambiguity.
            */

            // We also reached Editor element by using Application and Document.

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Editor ed = doc.Editor;

            // Each of these methods creates drawings and adds them as blocks to the AutoCAD database.
            // A Block is a single group of some drawings. (Blocks are also named as Block Definition in AutoCAD.)

            BuatOlustur();
            KomutatorOlustur();
            PrizOlustur();
            AnahtarOlustur();
            AplikOlustur();
            ArmaturOlustur();
            AvizeOlustur();
            LinyeLayer();
            SortiLayer();
                        
            // By using Editor we can communicate with users in AutoCAD application.
            ed.WriteMessage("\nRehber için LSGuide komutunu kullanınız");
        }

        [CommandMethod("Linye")]       // Line in English
        public void LinyeCiz()         // This method is used to draw electrical lines
        {
            // In order to use layers, drawings (line, circle ...), etc. we must reach
            // the Database element in the hierarchy. We reach it with the Document element.

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            ObjectId prevLayer = db.Clayer;             // Get the the ID of the latest selected layer by user.           
            Point3d lastpoint = new Point3d(0, 0, 0);   // Use this point to continue drawing from the last drawed line.
            Entity prev_ent = null;                     // Use it as the latest selected line as user.

            double offset = 0;   // Will decide the distance between walls and lines.
            double length = 0;   // Will be the total length of the lines.

            int offset_sign = 1;   // If we approach to the wall from right side or upper side
                                   // the x or y value will decrease so it will be -1, if we approach from
                                   // the lower or left side x or y value will increase so it will be 1.

            int once = 1;    // Will be used to execute specific code for only 1 time.
            int wloop = 1;   // Will continue to draw unless we cancel it.

            while (wloop == 1)
            {
                // Use transaction so we can commit changes
                using (Transaction trs = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt;   // Reach BlockTable in order to reach BlockTableRecord.
                    bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable; 

                    BlockTableRecord btr;   // Reach BlockTableRecord in order to add drawings to the database.
                    btr = trs.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    // Reach LayerTable so we can reach layers.
                    LayerTable layerTable = trs.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    try   // If we get error, execute catch line.
                    {
                        if (once == 1)   // Execute only once.
                        {
                            once = 0;    // Won't execute again.

                            if (layerTable.Has("Linye (LS)"))                  // If there is specific named layer...
                            {
                                ObjectId layerId = layerTable["Linye (LS)"];   // Get the ID of the named layer.
                                db.Clayer = layerId;                           // Change current layer to the mentioned layer.

                                ed.WriteMessage("Mevcut katman değiştirildi: " + "Linye (LS)");   // Inform user.
                            }

                            else // If the mentioned layer is not exist...
                            {
                                ed.WriteMessage("Katman bulunamadı: " + "Linye (LS)");
                            }

                            PromptPointOptions ppo = new PromptPointOptions("");   // Reach point options.
                            ppo.Message = ("\nBaşlangıç noktasını seçiniz:");   // Add a message to communicate with the user.

                            PromptPointResult ppr = ed.GetPoint(ppo);   // With using ppo, let the user select a point.
                            Point3d startingpoint = ppr.Value;          // Get the selected point value.

                            if (ppr.Status == PromptStatus.Cancel) // If the user cancelled (used "ESC" button) selection...
                            {
                                trs.Dispose();           // End transaction

                                db.Clayer = prevLayer;   // Change the layer to the previous one.
                                wloop = 0;               // End loop
                                
                                return;                  // Exit Loop
                            }

                            ppo.Message = ("\nİkinci noktayı seçiniz, bu nokta uzaklık belirleyecektir:");
                            ppo.UseBasePoint = true;         // Draw temporary indicator between mouse and specific point.
                            ppo.BasePoint = startingpoint;   // Use startingpoint as basepoint.
                            ppr = ed.GetPoint(ppo);

                            if (ppr.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                wloop = 0;

                                return;
                            }

                            Point3d endingpoint = ppr.Value;
                            lastpoint = ppr.Value;

                            Line startingline = new Line(startingpoint, endingpoint);   // Draw a line between the points.

                            length += startingline.Length; // Length = 0 + length of the line
                                
                            offset = length;   // Offset value will be same with the first drawed line.
                            ed.WriteMessage("\nOffset değeri: " + offset);

                            btr.AppendEntity(startingline);                    // Add the line to the drawings database.
                            trs.AddNewlyCreatedDBObject(startingline, true);   // Add the new object to the transaction.

                        }   // End of the "if (once == 1)"

                        else
                        {                            
                            PromptSelectionOptions pso = new PromptSelectionOptions();      // Reach selection options.
                            pso.SingleOnly = true;   // User can only select one line.
                            pso.MessageForAdding = "\nParalel çizilecek duvarı seçiniz:";   // Add a message to the user.
                            PromptSelectionResult psr = ed.GetSelection(pso);               // Get the selected "thing".

                            if (psr.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                wloop = 0;
                                ed.WriteMessage("\nLinye hattı uzunluğu: " + length);   // Show the total length of the lines.

                                string symst = "Buat (LS)";       // symst = symbol string
                                SembolCiz(symst, lastpoint, 0);   // This method gets the block "symst" from block table
                                                                  // and adds it to the drawing database.

                                return;                           // Exit the loop
                            }

                            SelectionSet sset = psr.Value;   // Get the selected "things" to a variable.

                            if (sset != null)   // If the selection exists...
                            {
                                SelectedObject sobj = sset[0];   // Since we only let the user select one selection
                                                                 // we only need to get 0th index.
                                if (sobj != null)   // If the object exists...
                                {
                                    // Reach the object (entity) from transaction
                                    Entity ent = trs.GetObject(sobj.ObjectId, OpenMode.ForRead) as Entity;
                                    Type objtype = ent.GetType();   // Get the type of the object.

                                    if (objtype != typeof(Line))   // We only work with lines
                                    {
                                        ed.WriteMessage("\nSadece line tipindeki varlıklar için çalışır.");
                                    }

                                    else
                                    {
                                        /*
                                            Extents3d ext = ent.GeometricExtents;
                                            Point3d firstpoint = ext.MinPoint;
                                            Point3d secondpoint = ext.MaxPoint;

                                         * At first, I used these lines but they are kinda wrong because
                                         * if our coordinates are starting (5, 7, 0) and ending (3, 9, 0)
                                         * ext.minPoint will return (3, 7, 0) points and
                                         * ext.maxPoint will return (5, 9, 0) points.
                                         
                                         * Since we are working with coordinates that do not change
                                         * these lines worked perfectly but it is wrong so we must use
                                         * correct lines.
                                        */

                                        Line linecoor = (Line)ent.Clone();          // Clone the entity.
                                        Point3d firstpoint = linecoor.StartPoint;   // Get one of the coordinates.
                                        Point3d secondpoint = linecoor.EndPoint;    // Get the other coordinates.

                                        double firstX = firstpoint.X;   // Get the x coordinate.
                                        double firstY = firstpoint.Y;   // Get the y coordinate.
                                        double secondX = secondpoint.X;
                                        double secondY = secondpoint.Y;
                                        double lastX = lastpoint.X;
                                        double lastY = lastpoint.Y;

                                        int axis = Axis(firstpoint, secondpoint);   // Check if it is horizontal or vertical.

                                        if (axis == 0) // none
                                        {
                                            ed.WriteMessage("\nSadece yatay veya dikey line için çalışır. Eğer bu durumlardan eminseniz bu seçimi silip tekrar çiziniz.");
                                        }

                                        else if (axis == 1) // Horizontal - x coordinates changes
                                        {
                                            // firstpoint.Y = secondpoint.Y, so we need to approach
                                            // y coordinate with the offset length.

                                            // First we need to check if we are approaching from
                                            // the upper or lower coordinates in order to find if
                                            // offset is positive or negative.

                                            if (prev_ent == ent)   // If the selected entity is the same as the previous one...
                                            {
                                                // Create a line with the offset to the wall
                                                Line line = new Line(lastpoint, new Point3d(lastX, firstY - offset * offset_sign, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                // Save the last point
                                                lastpoint = new Point3d(lastX, firstY - offset * offset_sign, 0);
                                            }

                                            else if (lastY > firstY) // Approach from upper point
                                            {
                                                offset_sign = 1;

                                                Line line = new Line(lastpoint, new Point3d(lastX, firstY + offset * offset_sign, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                lastpoint = new Point3d(lastX, firstY + offset * offset_sign, 0);
                                            }

                                            else if (lastY < firstY) // Approach from lower point
                                            {
                                                offset_sign = -1;

                                                Line line = new Line(lastpoint, new Point3d(lastX, firstY + offset * offset_sign, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                lastpoint = new Point3d(lastX, firstY + offset * offset_sign, 0);
                                            }
                                        }

                                        else if (axis == 2) // vertical - y coordinates changes
                                        {
                                            // So we need to approach x coordinates

                                            if (prev_ent == ent)
                                            {
                                                Line line = new Line(lastpoint, new Point3d(firstX - offset * offset_sign, lastY, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                lastpoint = new Point3d(firstX - offset * offset_sign, lastY, 0);
                                            }

                                            else if (lastX > firstX)
                                            {
                                                offset_sign = 1;
                                                Line line = new Line(lastpoint, new Point3d(firstX + offset * offset_sign, lastY, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                lastpoint = new Point3d(firstX + offset * offset_sign, lastY, 0);
                                            }

                                            else if (lastX < firstX)
                                            {
                                                offset_sign = -1;
                                                Line line = new Line(lastpoint, new Point3d(firstX + offset * offset_sign, lastY, 0));

                                                btr.AppendEntity(line);
                                                trs.AddNewlyCreatedDBObject(line, true);

                                                length += line.Length;

                                                lastpoint = new Point3d(firstX + offset * offset_sign, lastY, 0);
                                            }
                                        }

                                        prev_ent = ent;   // Save the last entity
                                    }
                                }

                                else
                                {
                                    ed.WriteMessage("\nVarlık bulunamadı.");
                                }

                            }

                            else
                            {
                                ed.WriteMessage("\nVarlık seçiniz.");
                            }


                        }

                    }   // End of try

                    catch   // If any error occurs...
                    {
                        db.Clayer = prevLayer;   // Return to the previous layer
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("\nProblem oluştu");
                    }

                    // We are creating a transaction and commiting in every loop
                    // because IDK why but I couldn't use transient method in this
                    // command. By commiting every time the user can see the changes.

                    // That won't cause much problems but performance
                    // issues but it does not impact that much so why not.

                    trs.Commit();   // Commit changes

                }   // End of transaction

            }   // End of while loop

        }

        [CommandMethod("Sorti")]       // Branch Line (?) in English
        public void SortiCiz()         // This method is used to draw branch lines (?).
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            Point3d buatCenter = new Point3d(0, 0, 0);    // This point will be used for drawing lines.
            Point3d lastPointEl = new Point3d(0, 0, 0);   // This point will be the last used point.

            ObjectId prevLayer = db.Clayer;

            int wloop = 1;
            int once = 1;

            double totalLength = 0;

            string kws = "";   // Will state the drawing mode.


            while (wloop == 1)
            {
                using (Transaction trs = db.TransactionManager.StartTransaction())
                {
                    BlockTable bt;
                    bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                    BlockTableRecord btr;
                    btr = trs.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                    LayerTable layerTable = trs.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                    try
                    {
                        if (once == 1)   // Executes only one time.
                        {
                            once = 0;

                            if (layerTable.Has("Sorti (LS)"))
                            {
                                ObjectId layerId = layerTable["Sorti (LS)"];
                                db.Clayer = layerId;
                                ed.WriteMessage("Mevcut katman değiştirildi: " + "Sorti (LS)");
                            }

                            else
                            {
                                ed.WriteMessage("Katman bulunamadı: " + "Sorti (LS)");
                            }

                            PromptPointOptions ppo = new PromptPointOptions("");
                            ppo.Message = ("\nBuatın merkez noktasını seçiniz:");

                            PromptPointResult ppr = ed.GetPoint(ppo);
                            buatCenter = ppr.Value;

                            lastPointEl = buatCenter;

                            if (ppr.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                wloop = 0;

                                return;
                            }

                            PromptKeywordOptions pko = new PromptKeywordOptions("");   // Reach keyword options.
                            pko.Keywords.Add("Aydınlatma");   // Add a keyword that can be used by user.
                            pko.Keywords.Add("Elektrik");
                            pko.Keywords.Add("");
                            pko.Keywords.Default = "";   // If nothing is selected it will be default keyword.
                            pko.Message = ("\nAydınlatma sistemleri veya Elektrik sistemleri için ");

                            PromptResult pkr = ed.GetKeywords(pko);   // Get the user input
                            kws = pkr.StringResult;                   // Get the result

                            // We are getting this keyword in if (once == 1)
                            // because it will activate only one mod until
                            // the command ends. By that way the user won't
                            // need to select mode everytime.

                            if (pkr.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                wloop = 0;

                                return;
                            }
                        }   // End of if (once == 1)

                        if (kws == "Aydınlatma")   // If user input is "Aydınlatma"...
                        {
                            PromptPointOptions ppoDraw = new PromptPointOptions("");
                            ppoDraw.Message = ("\nAydınlatmanın merkez noktasını seçiniz:");

                            PromptPointResult ppr = ed.GetPoint(ppoDraw);
                            Point3d pointAy = ppr.Value;

                            if (ppr.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                ed.WriteMessage("\nToplam kablo uzunluğu: " + totalLength);

                                wloop = 0;

                                return;
                            }

                            Line aydinlatmaLine = new Line(buatCenter, pointAy);

                            double length = aydinlatmaLine.Length;   // Get the length of the drawed line
                            totalLength += length;

                            ed.WriteMessage("\nTek kablo uzunluğu: " + length);

                            btr.AppendEntity(aydinlatmaLine);
                            trs.AddNewlyCreatedDBObject(aydinlatmaLine, true);

                            PromptKeywordOptions pkoAy = new PromptKeywordOptions("");
                            pkoAy.Keywords.Add("Aplik");
                            pkoAy.Keywords.Add("aRmatur");
                            pkoAy.Keywords.Add("aVize");
                            pkoAy.Keywords.Add("");
                            pkoAy.Keywords.Default = "";
                            pkoAy.Message = ("\nAydınlatma sistemleri ");
                            string kwsAy = "";

                            PromptResult pkrAy = ed.GetKeywords(pkoAy);
                            kwsAy = pkrAy.StringResult;

                            if (pkrAy.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                ed.WriteMessage("\nToplam kablo uzunluğu: " + totalLength);

                                wloop = 0;

                                return;
                            }

                            if (kwsAy == "Aplik")
                            {
                                string symst = "Aplik (LS)";
                                SembolCiz(symst, pointAy, 0);
                            }

                            else if (kwsAy == "aRmatur")
                            {
                                string symst = "Armatur (LS)";
                                SembolCiz(symst, pointAy, 0);
                            }

                            else if (kwsAy == "aVize")
                            {
                                string symst = "Avize (LS)";
                                SembolCiz(symst, pointAy, 0);
                            }

                            else
                            {
                                wloop = 0;
                            }
                        }
                        
                        else if (kws == "Elektrik")
                        {
                            PromptPointOptions ppoEl = new PromptPointOptions("");
                            ppoEl.Keywords.Add("Priz");        // You can also add keyword input to the point options.
                            ppoEl.Keywords.Add("Komutator");   // By that way the user can choose a point or keyword.
                            ppoEl.Keywords.Add("Anahtar");
                            ppoEl.Keywords.Add("Buat");
                            ppoEl.Keywords.Add("Ciz");
                            ppoEl.Keywords.Add("");
                            ppoEl.Keywords.Default = "";
                            ppoEl.Message = ("\nHat çizmek veya elektrik elemanı yerleştirmek için nokta seçiniz ya da ");
                            ppoEl.UseBasePoint = true;
                            ppoEl.BasePoint = lastPointEl;

                            PromptPointResult pprEl = null;
                            Point3d pointEl = new Point3d(0, 0, 0);
                            string kwsEl = "";


                            pprEl = ed.GetPoint(ppoEl);
                            kwsEl = pprEl.StringResult;
                            pointEl = pprEl.Value;

                            if (pprEl.Status == PromptStatus.Cancel)
                            {
                                trs.Dispose();

                                db.Clayer = prevLayer;
                                ed.WriteMessage("\nToplam kablo uzunluğu: " + totalLength);

                                wloop = 0;

                                return;
                            }

                            else if (kwsEl == "Priz")
                            {
                                // The user will be asked for a point to place the electrical element.
                                // After that a line will be created alongside with the placed symbol.

                                PromptPointOptions ppoEl2 = new PromptPointOptions("");
                                ppoEl2.Message = ("\nElektrik elemanı yerleştirmek için için nokta seçiniz: ");
                                ppoEl2.UseBasePoint = true;
                                ppoEl2.BasePoint = lastPointEl;

                                PromptPointResult pprEl2 = ed.GetPoint(ppoEl2);
                                Point3d componentPoint = pprEl2.Value;

                                Line componentLine = new Line(lastPointEl, componentPoint);

                                double componentLength = componentLine.Length;
                                totalLength += componentLength;

                                btr.AppendEntity(componentLine);
                                trs.AddNewlyCreatedDBObject(componentLine, true);

                                ed.WriteMessage("\nTek kablo uzunluğu: " + componentLength);

                                string symst = "Priz (LS)";
                                SembolCiz(symst, componentPoint, 0);
                            }

                            else if (kwsEl == "Komutator")
                            {
                                PromptPointOptions ppoEl2 = new PromptPointOptions("");
                                ppoEl2.Message = ("\nElektrik elemanı yerleştirmek için için nokta seçiniz: ");
                                ppoEl2.UseBasePoint = true;
                                ppoEl2.BasePoint = lastPointEl;

                                PromptPointResult pprEl2 = ed.GetPoint(ppoEl2);
                                Point3d componentPoint = pprEl2.Value;

                                Line componentLine = new Line(lastPointEl, componentPoint);

                                double componentLength = componentLine.Length;
                                totalLength += componentLength;

                                btr.AppendEntity(componentLine);
                                trs.AddNewlyCreatedDBObject(componentLine, true);

                                ed.WriteMessage("\nTek kablo uzunluğu: " + componentLength);

                                string symst = "Komutator (LS)";
                                SembolCiz(symst, componentPoint, 0);
                            }

                            else if (kwsEl == "Anahtar")
                            {
                                PromptPointOptions ppoEl2 = new PromptPointOptions("");
                                ppoEl2.Message = ("\nElektrik elemanı yerleştirmek için nokta seçiniz: ");
                                ppoEl2.UseBasePoint = true;
                                ppoEl2.BasePoint = lastPointEl;

                                PromptPointResult pprEl2 = ed.GetPoint(ppoEl2);
                                Point3d componentPoint = pprEl2.Value;

                                Line componentLine = new Line(lastPointEl, componentPoint);

                                double componentLength = componentLine.Length;
                                totalLength += componentLength;

                                btr.AppendEntity(componentLine);
                                trs.AddNewlyCreatedDBObject(componentLine, true);

                                ed.WriteMessage("\nTek kablo uzunluğu: " + componentLength);

                                string symst = "Anahtar (LS)";
                                SembolCiz(symst, componentPoint, 0);
                            }

                            else if (kwsEl == "Buat")
                            {
                                PromptPointOptions ppoEl2 = new PromptPointOptions("");
                                ppoEl2.Message = ("\nElektrik elemanı yerleştirmek için nokta seçiniz: ");
                                ppoEl2.UseBasePoint = true;
                                ppoEl2.BasePoint = lastPointEl;

                                PromptPointResult pprEl2 = ed.GetPoint(ppoEl2);
                                Point3d componentPoint = pprEl2.Value;

                                Line componentLine = new Line(lastPointEl, componentPoint);

                                double componentLength = componentLine.Length;
                                totalLength += componentLength;

                                btr.AppendEntity(componentLine);
                                trs.AddNewlyCreatedDBObject(componentLine, true);

                                ed.WriteMessage("\nTek kablo uzunluğu: " + componentLength);

                                string symst = "Buat (LS)";
                                SembolCiz(symst, componentPoint, 0);
                            }

                            else if (kwsEl == "Ciz")   // If this keyword is selected the command will end.
                            {
                                db.Clayer = prevLayer;
                                ed.WriteMessage("\nToplam kablo uzunluğu: " + totalLength);   // Total length will be written.

                                wloop = 0;
                            }

                            else   // If the user selected a point the string will be "" so these lines will be executed.
                            {
                                // Creates a line, adds line length to the total length,
                                // returns the single line's length (not total) to the user.
                                
                                Line elektrikLine = new Line(lastPointEl, pointEl);
                                double length = elektrikLine.Length;
                                totalLength += length;

                                ed.WriteMessage("\nTek kablo uzunluğu: " + length);

                                btr.AppendEntity(elektrikLine);
                                trs.AddNewlyCreatedDBObject(elektrikLine, true);

                                lastPointEl = pointEl;
                            }
                        }

                        else   // End loop if the selection is not "Aydınlatma" or "Elektrik"
                        {
                            wloop = 0;
                        }

                    }

                    catch
                    {
                        db.Clayer = prevLayer;
                        Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog("\nProblem oluştu");
                    }

                    trs.Commit();
                }

            }

        }

        [CommandMethod("Aydinlat")]    // Illuminate in English
        public void Aydinlatma()       // This method calculates the area of the region within 4 points.
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                BlockTableRecord btr;
                btr = trs.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite) as BlockTableRecord;

                PromptPointOptions ppo = new PromptPointOptions("");
                PromptPointResult ppr;

                List<Point3d> corners = new List<Point3d>();   // Created a list of points

                ppo.Message = "\nAydınlatmak istediğiniz dörtgenin köşesini seçiniz:";

                ppr = ed.GetPoint(ppo);
                Point3d corner_first = ppr.Value;
                ppo.UseBasePoint = true;
                ppo.BasePoint = corner_first;
                corners.Add(corner_first);   // Add the coordinate to the list

                for (int kose = 1; kose < 4; kose++)   // We already got the 0th index so it will get 1 to 3.
                {
                    ppo.Message = "\nAydınlatmak istediğiniz dörtgenin diğer köşesini seçiniz: ";
                    ppr = ed.GetPoint(ppo);
                    Point3d corner = ppr.Value;

                    if (ppr.Status == PromptStatus.OK) corners.Add(corner);   // If we get a point input...
                    if (ppr.Status != PromptStatus.OK) return;                // If we dont get a point input end the command.
                }

                double x, y;
                Polyline temp_pline = new Polyline();   // Created a temporary polyline because
                                                        // we can only use .area function with polylines
                                                        // Area calculation could also be done with point
                                                        // coordinates but I wanted to take the easy way.

                                                        // But we won't add this polyline to the database
                                                        // so the user will not see it.

                for (int i = 0; i < corners.Count; i++)
                {
                    x = corners[i].X;   // Get the x coordinates
                    y = corners[i].Y;   // Get the y coordinates

                    temp_pline.AddVertexAt(i, new Point2d(x, y), 0, 0, 0);   // Add polyline's vertex (corner).
                }

                temp_pline.Closed = true;        // Polyline is closed so it will create a region.
                Double area = temp_pline.Area;   // Calculate the area of the region inside of the polyline.

                double x1, x2, x3, x4;
                double y1, y2, y3, y4;
                double mid_x, mid_y;
                double len_u, len_l, len_r, len_left; // lenght upper, lower, right, left
                double mid_u, mid_l, mid_r, mid_left; // middle upper, lower (x) ; right, left (y)

                x1 = corners[0].X; x2 = corners[1].X; x3 = corners[2].X; x4 = corners[3].X;   // Get the coordinates
                y1 = corners[0].Y; y2 = corners[1].Y; y3 = corners[2].Y; y4 = corners[3].Y;

                // We are calculating distances and mid point of the distances.
                // By that way we can find the center of the 4 points.

                len_r = Math.Sqrt(Math.Pow(x4 - x3, 2) + Math.Pow(y4 - y3, 2));
                len_l = Math.Sqrt(Math.Pow(x3 - x2, 2) + Math.Pow(y3 - y2, 2));
                len_left = Math.Sqrt(Math.Pow(x1 - x4, 2) + Math.Pow(y1 - y4, 2));  
                len_u = Math.Sqrt(Math.Pow(x2 - x1, 2) + Math.Pow(y2 - y1, 2));

                // Mid points of the adjacent points.
                mid_r = (y1 + y2) / 2; mid_l = (x2 + x3) / 2; mid_left = (y3 + y4) / 2; mid_u = (x4 + x1) / 2;

                // If the shape is not a regular quadrilateral (square, rectangle, etc.),
                // we will find the center by averaging the midpoints of the opposite sides.
                mid_x = (mid_u + mid_l) / 2; mid_y = (mid_r + mid_left) / 2;

                Point3d center = new Point3d(mid_x, mid_y, 0);   // State the center.

                double per = len_r + len_l + len_left + len_u; // Calculate the perimeter
                double scaled_per;   // Perimeter will be scaled with drawing scale.

                Circle circle = new Circle();   // Create circle.
                circle.SetDatabaseDefaults();   // Get the default values for circle.
                circle.Center = center;         // Change the default center to our value.
                circle.Radius = 7.5;            // Change the default radius to our value.

                btr.AppendEntity(circle);
                trs.AddNewlyCreatedDBObject(circle, true);

                double scaled_area;   // Area will also be scaled with the drawing scale.

                scaled_area = area / (50 * 50);
                scaled_per = per / 50;
                ed.WriteMessage("\n\n1/50 ölçek için alan: {0}", scaled_area);
                ed.WriteMessage("\n1/50 ölçek için çevre: {0}", scaled_per);

                scaled_area = area / (100 * 100);
                scaled_per = per / 100;
                ed.WriteMessage("\n\n1/100 ölçek için alan: {0}", scaled_area);
                ed.WriteMessage("\n1/100 ölçek için çevre: {0}", scaled_per);

                scaled_area = area / (200 * 200);
                scaled_per = per / 200;
                ed.WriteMessage("\n\n1/200 ölçek için alan: {0}", scaled_area);
                ed.WriteMessage("\n1/200 ölçek için çevre: {0}", scaled_per);


                trs.Commit();
            }

        }

        [CommandMethod("Buat")]        // Junction Box
        public void BuatCiz()
        {
            string symst = "Buat (LS)";

            // Since our method is SembolCiz(symbol name, drawing center, skip drawing center)
            // when we use the method with 1 value (default 1) of skip drawing center
            // it will skip the given drawing center point,
            // by that way we can let the user select the drawing point.
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Komutator")]   // Commutator Switch
        public void KomutatorCiz()
        {
            string symst = "Komutator (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Priz")]        // Socket
        public void PrizCiz()
        {
            string symst = "Priz (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Anahtar")]     // Switch
        public void AnahtarCiz()
        {
            string symst = "Anahtar (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Aplik")]       // Sconce
        public void AplikCiz()
        {
            string symst = "Aplik (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Armatur")]     // Armature (Lightning Fixture)
        public void ArmaturCiz()
        {
            string symst = "Armatur (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        [CommandMethod("Avize")]       // Chandelier
        public void AvizeCiz()
        {
            string symst = "Avize (LS)";
            SembolCiz(symst, new Point3d(0, 0, 0));
        }

        private void BuatOlustur()          // Create Junction Box
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            double radius = 2.5;
            Point3d center = new Point3d(0, 0, 0);

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Buat (LS)"))   // If there are not any block with the same name...
                {
                    using (BlockTableRecord btr = new BlockTableRecord())   // Create a new btr
                    {
                        btr.Name = "Buat (LS)";              // Name the block
                        btr.Origin = new Point3d(0, 0, 0);   // Set the origin of the block.

                       
                        // We can't add hatches to block because of a problem with autocad
                        // so instead of creating a hatch, we will draw nested circles. 
                        for (double rad = radius; rad > 0; rad -= radius / 20)
                        {
                            // With decreasing radius we can draw circles.
                            Circle circle = new Circle
                            {
                                Center = center,
                                Radius = rad,
                                ColorIndex = 1
                            };

                            btr.AppendEntity(circle);
                        }

                        bt.UpgradeOpen();   // We reached BlockTable as read only but we need
                                            // write permission so this line will be used for it. 
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nBuat bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void KomutatorOlustur()     // Create Commutator Switch
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Komutator (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Komutator (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Circle circle = new Circle
                        {
                            Center = new Point3d(0, 5, 0),
                            Radius = 5,
                            ColorIndex = 1
                        };

                        Line line_ra = new Line(new Point3d(2.5044, 9.3276, 0), new Point3d(8.1235, 23.2353, 0)); // right arm
                        Line line_re = new Line(new Point3d(8.1235, 23.2353, 0), new Point3d(17.6341, 20.1452, 0)); // right end
                        Line line_la = new Line(new Point3d(-2.5044, 9.3276, 0), new Point3d(-8.1235, 23.2353, 0));
                        Line line_le = new Line(new Point3d(-8.1235, 23.2353, 0), new Point3d(-17.6341, 20.1452, 0));

                        line_ra.ColorIndex = 1;
                        line_re.ColorIndex = 1;
                        line_la.ColorIndex = 1;
                        line_le.ColorIndex = 1;

                        btr.AppendEntity(circle);
                        btr.AppendEntity(line_ra);
                        btr.AppendEntity(line_re);
                        btr.AppendEntity(line_la);
                        btr.AppendEntity(line_le);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nKomütatör bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void PrizOlustur()          // Create Socket
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Priz (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Priz (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Arc arc = new Arc(new Point3d(30, 0, 0), 10, 1.570796, 4.712389); // arc = center, radius, start angle (radian), end angle (radian)

                        arc.ColorIndex = 1;

                        Line line_h = new Line(new Point3d(0, 0, 0), new Point3d(20, 0, 0));
                        Line line_v = new Line(new Point3d(20, -7.5, 0), new Point3d(20, 7.5, 0));

                        line_h.ColorIndex = 1;
                        line_v.ColorIndex = 1;

                        btr.AppendEntity(arc);
                        btr.AppendEntity(line_h);
                        btr.AppendEntity(line_v);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nPriz bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void AnahtarOlustur()       // Create Switch
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Anahtar (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Anahtar (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Circle circle = new Circle
                        {
                            Center = new Point3d(0, 5, 0),
                            Radius = 5,
                            ColorIndex = 1
                        };

                        Line line_a = new Line(new Point3d(2.5044, 9.3276, 0), new Point3d(12.1462, 29.8182, 0));
                        Line line_e = new Line(new Point3d(12.1462, 29.8182, 0), new Point3d(20.1326, 23.8001, 0));

                        line_a.ColorIndex = 1;
                        line_e.ColorIndex = 1;

                        btr.AppendEntity(circle);
                        btr.AppendEntity(line_a);
                        btr.AppendEntity(line_e);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nAnahtar bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void AplikOlustur()         // Create Sconce
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Aplik (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Aplik (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Line line_h = new Line(new Point3d(0, 0, 0), new Point3d(25, 0, 0));
                        Line line_v = new Line(new Point3d(17.5, -7.5, 0), new Point3d(17.5, 7.5, 0));
                        Line line_x_one = new Line(new Point3d(19.6967, 5.3033, 0), new Point3d(30.3033, -5.3033, 0));
                        Line line_x_two = new Line(new Point3d(30.3033, 5.3033, 0), new Point3d(19.6967, -5.3033, 0));

                        line_h.ColorIndex = 1;
                        line_v.ColorIndex = 1;
                        line_x_one.ColorIndex = 1;
                        line_x_two.ColorIndex = 1;

                        btr.AppendEntity(line_h);
                        btr.AppendEntity(line_v);
                        btr.AppendEntity(line_x_one);
                        btr.AppendEntity(line_x_two);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nAplik bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void ArmaturOlustur()       // Create Armature
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Armatur (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Armatur (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Line line_x_one = new Line(new Point3d(5.3033, -5.3033, 0), new Point3d(-5.3033, 5.3033, 0));
                        Line line_x_two = new Line(new Point3d(-5.3033, -5.3033, 0), new Point3d(5.3033, 5.3033, 0));

                        line_x_one.ColorIndex = 1;
                        line_x_two.ColorIndex = 1;

                        btr.AppendEntity(line_x_one);
                        btr.AppendEntity(line_x_two);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nArmatur bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void AvizeOlustur()         // Create Chandelier
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                if (!bt.Has("Avize (LS)"))
                {
                    using (BlockTableRecord btr = new BlockTableRecord())
                    {
                        btr.Name = "Avize (LS)";
                        btr.Origin = new Point3d(0, 0, 0);

                        Circle circle = new Circle
                        {
                            Center = new Point3d(0, 0, 0),
                            Radius = 7.5,
                            ColorIndex = 1
                        };

                        Line line_x_one = new Line(new Point3d(5.3033, -5.3033, 0), new Point3d(-5.3033, 5.3033, 0));
                        Line line_x_two = new Line(new Point3d(-5.3033, -5.3033, 0), new Point3d(5.3033, 5.3033, 0));

                        line_x_one.ColorIndex = 1;
                        line_x_two.ColorIndex = 1;

                        btr.AppendEntity(circle);
                        btr.AppendEntity(line_x_one);
                        btr.AppendEntity(line_x_two);

                        bt.UpgradeOpen();
                        bt.Add(btr);
                        trs.AddNewlyCreatedDBObject(btr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nAvize bloğu zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private void LinyeLayer()   // Line Layer
        {
            // In here, like we add drawings to the database
            // we will add layer to database in LayerTable.

            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                LayerTable lt;
                lt = trs.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lt.Has("Linye (LS)"))   // If there are not any layer with the same name...
                {
                    using (LayerTableRecord ltr = new LayerTableRecord())
                    {
                        ltr.Name = "Linye (LS)";   // Layer name

                        // Define layer color.
                        ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 80);

                        lt.UpgradeOpen();
                        lt.Add(ltr);
                        trs.AddNewlyCreatedDBObject(ltr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nLinye katmanı zaten mevcut.");
                }

                trs.Commit();
            }
        }
        
        private void SortiLayer()   // Branch Line Layer
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                LayerTable lt;
                lt = trs.GetObject(db.LayerTableId, OpenMode.ForRead) as LayerTable;

                if (!lt.Has("Sorti (LS)"))
                {
                    using (LayerTableRecord ltr = new LayerTableRecord())
                    {
                        ltr.Name = "Sorti (LS)";
                        ltr.Color = Autodesk.AutoCAD.Colors.Color.FromColorIndex(ColorMethod.ByAci, 80);

                        lt.UpgradeOpen();
                        lt.Add(ltr);
                        trs.AddNewlyCreatedDBObject(ltr, true);
                    }
                }

                else
                {
                    ed.WriteMessage("\nSorti katmanı zaten mevcut.");
                }

                trs.Commit();
            }
        }

        private static int Axis(Point3d start, Point3d end)
        {
            // Calculate the difference of the points
            double x = end.X - start.X;
            if (x < 0) x = 0 - x;   // If the result is negative make it positive

            double y = end.Y - start.Y;
            if (y < 0) y = 0 - y;
            
            if (x < y) // vertical
            {
                return 2;
            }

            else if (y < x) // horizontal
            {
                return 1;
            }

            else // non spatial
            {
                return 0;
            }
        }

        public void SembolCiz(string symst, Point3d drawPoint, int skipPoint = 1)
        {
            Document doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;
            Database db = doc.Database;
            Editor ed = doc.Editor;

            int wloop = 1;

            using (Transaction trs = db.TransactionManager.StartTransaction())
            {
                BlockTable bt;
                bt = trs.GetObject(db.BlockTableId, OpenMode.ForRead) as BlockTable;

                // With TransientManager we can draw transient images that won't be
                // added to the database but will be shown without committing
                AcGi.TransientManager ctm = AcGi.TransientManager.CurrentTransientManager;

                IntegerCollection ints = new IntegerCollection(new int[] { });   // Will be used for transient

                try
                {
                    if (skipPoint != 0)
                    {
                        ObjectId brecId = bt[symst]; // block record Id = brecId
                        BlockReference bref = new BlockReference(Point3d.Origin, brecId);

                        // Add block as transient.
                        ctm.AddTransient(bref, AcGi.TransientDrawingMode.DirectShortTerm, 128, ints);   

                        Matrix3d curUCSMatrix = ed.CurrentUserCoordinateSystem;        // Reach coordinate system
                        CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;   // Will be used to rotate symbol.

                        // When user moves mouse, it will update
                        // the location of the transient image.
                        PointMonitorEventHandler handler =   
                          delegate (object sender, PointMonitorEventArgs e)
                          {
                              bref.Position = e.Context.RawPoint;   // Update location.

                              ctm.UpdateTransient(bref, ints);      // Update transient image.
                          };

                        ed.PointMonitor += handler;  // Catching the mouse move event.

                        PromptPointOptions ppo = new PromptPointOptions("");
                        ppo.Keywords.Add("Rotate");
                        ppo.Keywords.Add("rEverse");
                        ppo.Keywords.Add("");
                        ppo.Keywords.Default = "";
                        ppo.Message = ("\nBaşlangıç noktasını seçiniz yada çevirmek için ");

                        if (symst == "Avize (LS)" || symst == "Armatur (LS)" || symst == "Buat (LS)")
                        {                            
                            ppo.Message = ("\nMerkez noktasını seçiniz: ");
                            ppo.Keywords.Clear(); // There symbols do not have any direction so it is useless to rotate them.
                        }

                        PromptPointResult ppr = null;
                        Point3d point = new Point3d(0, 0, 0);
                        string kws = "";

                        try
                        {
                            while (wloop == 1)
                            {
                                ppr = ed.GetPoint(ppo);
                                kws = ppr.StringResult;
                                point = ppr.Value;

                                if (kws == "Rotate")
                                {
                                    // Rotate the block clockwise
                                    bref.TransformBy(
                                        Matrix3d.Rotation(
                                            270 * pi / 180,
                                            curUCS.Zaxis,
                                            bref.Position));

                                    // 270 * pi / 180 means convert 270 degree to radian.
                                }

                                else if (kws == "rEverse")
                                {
                                    // Counter clockwise
                                    bref.TransformBy(
                                        Matrix3d.Rotation(
                                            90 * pi / 180,
                                            curUCS.Zaxis,
                                            bref.Position));
                                }

                                else   // End loop
                                {
                                    wloop = 0;
                                }
                            }
                        }

                        finally
                        {
                            ed.PointMonitor -= handler;   // End mouse move event catcher.

                            ctm.EraseTransient(bref, ints);   // Erase the transient image.
                        }

                        if (brecId != ObjectId.Null)   // If brec exists.
                        {
                            if (ppr.Status == PromptStatus.OK)
                            {
                                using (bref)
                                {
                                    // Place the block.
                                    BlockTableRecord btr;
                                    btr = trs.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                                    btr.AppendEntity(bref);
                                    trs.AddNewlyCreatedDBObject(bref, true);
                                }
                            }
                        }
                    }

                    else   // If we don't skip given drawing point for SembolCiz(sysmst, drawPoint, skipPoint)
                    {
                        ObjectId brecId = bt[symst];   // block record Id = brecId
                        BlockReference bref = new BlockReference(Point3d.Origin, brecId);

                        bref.Position = drawPoint;     // Position is fixed 

                        ctm.AddTransient(bref, AcGi.TransientDrawingMode.DirectShortTerm, 128, ints);   // Add transient

                        Matrix3d curUCSMatrix = ed.CurrentUserCoordinateSystem;
                        CoordinateSystem3d curUCS = curUCSMatrix.CoordinateSystem3d;

                        if (symst == "Avize (LS)" || symst == "Armatur (LS)" || symst == "Buat (LS)")
                        {
                            if (brecId != ObjectId.Null)
                            {
                                using (bref)
                                {
                                    BlockTableRecord btr;
                                    btr = trs.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                                    btr.AppendEntity(bref);
                                    trs.AddNewlyCreatedDBObject(bref, true);
                                }                                
                            }
                        }

                        else
                        {
                            PromptKeywordOptions pko = new PromptKeywordOptions("");
                            pko.Keywords.Add("Rotate");
                            pko.Keywords.Add("rEverse");
                            pko.Keywords.Add("Ciz");
                            pko.Keywords.Default = "Ciz";
                            pko.Message = ("\nÇevirmek ya da Çizimi tamamlamak için ");

                            PromptResult pkr = null;
                            string kws = "";

                            while (wloop == 1)
                            {
                                pkr = ed.GetKeywords(pko);
                                kws = pkr.StringResult;

                                if (kws == "Rotate")
                                {
                                    bref.TransformBy(
                                        Matrix3d.Rotation(
                                            270 * pi / 180,
                                            curUCS.Zaxis,
                                            bref.Position));

                                    ctm.UpdateTransient(bref, ints);   // Since we don't catch any event, we
                                                                       // should update the transient manually
                                }

                                else if (kws == "rEverse")
                                {
                                    bref.TransformBy(
                                        Matrix3d.Rotation(
                                            90 * pi / 180,
                                            curUCS.Zaxis,
                                            bref.Position));

                                    ctm.UpdateTransient(bref, ints);
                                }

                                else
                                {
                                    wloop = 0;
                                }
                            }

                            if (brecId != ObjectId.Null)
                            {
                                using (bref)
                                {
                                    BlockTableRecord btr;
                                    btr = trs.GetObject(db.CurrentSpaceId, OpenMode.ForWrite) as BlockTableRecord;

                                    ctm.EraseTransient(bref, ints);

                                    btr.AppendEntity(bref);
                                    trs.AddNewlyCreatedDBObject(bref, true);
                                }
                            }
                        }
                    }
                }

                catch
                {
                    Autodesk.AutoCAD.ApplicationServices.Application.ShowAlertDialog(symst + " bloğu bulunamadı.. komutunu giriniz.");
                }

                trs.Commit();
            }

        }

    }
  
    public class LSGuide
    {
        private static PaletteSet _ps = null;   // Declare a static variable to hold the palette set

        [PaletteMethod]   // This line states that the method creates a palette
        [CommandMethod("LSGuide")]
        public void Palette()   // This method creates a palette set in AutoCAD
        {
            // This method creates a palette set in the assembly but
            // it's design won't be the same with our desire.
            // so we will first create a palette set with this command,
            // then we will make it unvisible
            
            if (_ps == null)   // If there is not any palette set...
            {
                var doc = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager.MdiActiveDocument;

                if (doc == null) return;   // If there is no active document, end comamnd.

                var ed = doc.Editor;

                var asm = Assembly.GetExecutingAssembly();           // Get the executing assembly.
                var type = asm.GetType("Line_and_Symbol.LSGuide");   // Get the type of the assembly.

                if (type == null)
                {
                    ed.WriteMessage("\nCould not find the command class.");

                    return;
                }

                var bs = new List<WinForms.Button>();   // Hold the buttons.
                var i = 1;                              // This variable will help positioning buttons.

                foreach (var m in type.GetMethods())   // Get all the methods in this class (public class LSGuide).
                {
                    var cmdName = "";      // Store Command names.
                    var palette = false;   // States if the method will be added to palette.

                    foreach (var a in m.CustomAttributes)   // Iterate over the custom attributes applied to the method
                    {
                        // Check if the attribute is CommandMethodAttribute
                        if (a.AttributeType.Name == "CommandMethodAttribute")
                        {
                            // Get the command name from the attribute constructor argument
                            cmdName = (string)a.ConstructorArguments[0].Value;
                        }

                        else if (a.AttributeType.Name == "PaletteMethod")   // Check if the attribute is PaletteMethod
                        {
                            palette = true;
                        }
                    }

                    if (palette)   // If the method should be added to the palette...
                    {

                        var b = new WinForms.Button();   // Create a button.

                        b.SetBounds(50, 40 * i, 100, 30);   // Set the position and size of the button.

                        if (String.IsNullOrEmpty(cmdName))   // If the command name is empty or not specified...
                        {
                            b.Text = m.Name;   // Name of the button.

                            // Catch the button click event.
                            b.Click +=
                              (s, e) =>
                              {
                                  var b2 = (WinForms.Button)s;
                                  var mi = type.GetMethod(b2.Text);

                                  if (mi != null)
                                  {
                                      mi.Invoke(this, null);
                                  }
                              };
                        }

                        else
                        {
                            b.Text = cmdName;

                            // Catch the button click event and execute asynchronously.
                            b.Click +=
                              async (s, e) =>
                              {
                                  var dm = Autodesk.AutoCAD.ApplicationServices.Application.DocumentManager;
                                  var doc2 = dm.MdiActiveDocument;
                                  if (doc2 == null) return;

                                  var ed2 = doc2.Editor;

                                  await dm.ExecuteInCommandContextAsync(

                                  async (obj) =>
                                  {
                                      // Execute the command asynchronously in the command context.
                                      await ed2.CommandAsync("_." + cmdName);
                                  },
                                    null
                                  );
                              };

                        }

                        bs.Add(b);   // Add button to the list.

                        i++;
                    }

                }

                var uc = new WinForms.UserControl();   // Create user control.
                uc.Controls.AddRange(bs.ToArray());    // Add buttons to the user control.

                // Create a new PaletteSet with a name and GUID.

                // GUID is an acronym for Globally Unique Identifier, in short,
                // it is a random set of numbers and letters with a very low
                // probality to generate the same GUID so the palettes won't
                // have the same GUID and duplicate.

                _ps = new PaletteSet("PC", new Guid("4E99370C-4E9B-4A9C-A150-ED74359275CC"));   

                _ps.Add("CMDPAL", uc);                           // Add the user control to the palette set.
                _ps.MinimumSize = new Size(200, (i + 1) * 40);   // Set min size of the palette.

                _ps.DockEnabled = (DockSides)(DockSides.Left | DockSides.Right); // Enable docking.
            }

            // _ps.Visible = true;   we don't use this palette so this line won't be used.

            Empty();
        }

        private void Empty()
        {
            // We have an empty palette because when we close
            // main palette (that we generated with Palette() method)
            // with close button, it will cause errors.

            // So with using this method we will create an empty palette.
            // So the close button will close empty page instead of the
            // main palette

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var newPalette = new PaletteSet("Ana Menü");
            newPalette.Add("GUIDEPAL", messageControl);
            newPalette.Size = new Size(200, 150);

            // newPalette.Visible = true;   no need to use

            HomePage(newPalette);
        }

        private void HomePage(PaletteSet newPalette)
        {
            // Close the previous palette
            ClosePalette(newPalette);

            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;   // Background color.

            // This enables automatic scrolling for the UserControl
            // when its content exceeds its visible area.
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();   // Creates a textbox.
            messageBody.Text =    "\n   Merhaba!\n"
                                + "\n   Hat ve Sembol rehberine hoş geldiniz! "
                                + "Şuan ana menüdesiniz. "
                                + "Aşağıdaki butonlara basarak rehberi kullanabilirsiniz.";
            messageBody.Dock = WinForms.DockStyle.Top;                          // The text box will we at the top
            messageBody.ReadOnly = true;                                        // User can't edit the text
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;   // Vertical scrollbar

            // If there is no space at the end of the line this function
            // will transfer the word to the next line.
            messageBody.WordWrap = true;                                        
            messageBody.Height = 170;                   // height of the textbox
            messageControl.Controls.Add(messageBody);   // Add messages to usercontrol

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Ana Menü");      // Create a palette set and name it
            newPalette.Add("GUIDEPAL", messageControl);   // Palette tag, only used when multiple palettes are opened.
            newPalette.Size = new Size(200, 150);         // Default palette size

            LSButton(messageControl, newPalette);         // Add Buttons
            CommandsButton(messageControl, newPalette);   
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;   // Make palette visible
        }

        private void LSPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Hat ve Sembol (LS), "
                                + "elektrik mühendislerinin çizimlerine "
                                + "yardımcı olmak amacıyla yazılmış, "
                                + "içeriğinde linye hattı, sorti hattı ve "
                                + "elektrik sembolleri çizdirme komutları ile "
                                + "aydınlatma hesabı yapan komutları olan "
                                + "bir AutoCAD eklentisidir.";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 170;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Hat ve Sembol Nedir?");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            CommandsButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void CommandsPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Linye hattı çizdirme, sorti "
                                + "hattı çizdirme ve aydınlatma hesabı "
                                + "komutu olarak 3 çeşit komut ile "
                                + "her sembolün ayrı ayrı çizimleri "
                                + "için sembol komutları bulunmaktadır. "
                                + "\n\n   Bu komutları aşağıdaki butonlar "
                                + "aracılığı ile inceleyebilirsiniz. "
                                + "\n\n   UYARI: Komutları kullanırken türkçe "
                                + "karakterler kullanmak sorunlara sebep olabilir.";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 150;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Komutlar");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            LinyeButton(messageControl, newPalette);
            SortiButton(messageControl, newPalette);
            EnlightButton(messageControl, newPalette);
            SymbolButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void LinyeCommandPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Linye komutu, komut satırına "
                                + "\"Linye\" yazarak kullanılır. "
                                + "\n\n   Linye komutu yazıldığında mevcut "
                                + "çizim katmanı \"Linye\" katmanı ile "
                                + "değiştirilecektir ve komut bittiğinde ise "
                                + "en son seçili olan katmana geçiş sağlanacaktır. "
                                + "\n\n   Linye komutunu kullandıktan sonra "
                                + "kullanıcıdan bir nokta istenecektir, bu nokta "
                                + "başlangıç noktası olacaktır, bu nokta seçildikten "
                                + "sonra ikinci bir nokta seçilmesi istenecektir. "
                                + "Birinci seçilen noktadan ikinci noktaya kablo çekilecek "
                                + "ve bu kablonun uzunluğu çizilecek hat ile seçilen "
                                + "duvarlar arasındaki mesafeyi (offset) belirleyecektir. "
                                + "\n\n   İkinci nokta da seçildikten sonra kullanıcı \"ESC\" "
                                + "tuşuna basana kadar bir duvar seçilmesi istenecektir. "
                                + "Seçilen duvar yatay ise hat dikey olarak yaklaşacak, "
                                + "duvar dikey ise hat yatay olarak yaklaşacaktır. "
                                + "Eğer aynı duvar iki kez seçilirse kablo "
                                + "duvarın içinden geçecektir. "
                                + "\"ESC\" tuşuna basıldıktan sonra ise hattın sonuna "
                                + "buat yerleştirilerek hat uzunluğu verilecektir."
                                + "\n\n   UYARI: Uzunluk hesabını yaparken ölçekleri dikkate alınız!";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 200;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Linye Komutu");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            BackButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void SortiCommandPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Sorti komutu, komut satırına "
                                + "\"Sorti\" yazarak kullanılır. "
                                + "\n\n   Sorti komutu yazıldığında mevcut "
                                + "çizim katmanı \"Sorti\" katmanı ile "
                                + "değiştirilecektir ve komut bittiğinde ise "
                                + "en son seçili olan katmana geçiş sağlanacaktır. "
                                + "\n\n   Sorti komutunu kullandıktan sonra "
                                + "kullanıcıdan bir nokta istenecektir, bu nokta "
                                + "başlangıç noktası olacaktır, bu nokta seçildikten "
                                + "sonra kullanıcıdan \"Aydınlatma\" veya \"Elektrik\" "
                                + "komutlarından biri istenecektir, bu komutlar "
                                + "iki moddan birini etkinleştirecektir. "
                                + "\n\n   Aydınlatma modunda kullanıcıdan "
                                + "bir nokta seçimi ile aydınlatma elemanı "
                                + "seçimi istenecektir. Başlangıç noktasından "
                                + "belirtilen noktaya çizgi çekilerek seçilen eleman "
                                + "yerleştirilecektir ve kablo uzunluğu verilecektir, "
                                + "seçilen eleman \"R\" veya \"E\" tuşları ile döndürülebilir. "
                                + "\"ESC\" tuşuna basılana kadar nokta seçimi ve "
                                + "eleman seçimi tekrar ederek başlangıç noktasından "
                                + "çizim yapılmaya devam edilecektir."
                                + "\n\n   Elektrik modunda ise kullanıcıdan "
                                + "bir nokta seçimi veya elektrik elemanı "
                                + "seçimi istenecektir. Nokta seçimi yapılırsa "
                                + "birinci noktadan ikinci noktaya bir hat çekilecek "
                                + "ve kablo uzunluğu verilecektir, eleman seçimi yapılırsa "
                                + "elemanın yerleştirileceği noktanın seçilmesi istenecektir. "
                                + "Nokta seçimi yapıldıktan sonra \"R\" veya \"E\" tuşları "
                                + "ile elemanı döndürme işlemi yapılabilir. "
                                + "\"ESC\" tuşuna basılana kadar son hat noktasından çizim "
                                + "devam edecektir."
                                + "\n\n   UYARI: Uzunluk hesabını yaparken ölçekleri dikkate alınız!";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 200;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Sorti Komutu");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            BackButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void EnlightCommandPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Aydınlatma komutu, komut satırına "
                                + "\"Aydınlat\" yazarak kullanılır. "
                                + "\n\n   Aydınlat komutu sizden sırasıyla 4 nokta "
                                + "seçmenizi isteyecektir. Seçilen bu noktalar ile "
                                + "alan hesabı yapılacak ve merkeze bir adet çember "
                                + "yerleştirilecektir. Bu çemberin merkezi, seçilen 4 "
                                + "noktanın merkezi ile aynıdır."
                                + "\n\n   Alan hesabına göre aydınlatma "
                                + "hesabı yapabilir, yerleştirilen çemberi "
                                + "odanın merkezi olarak referans alarak " 
                                + "odanın içine ışıklandırmaları yerleştirebilirsiniz. "
                                + "\n\n   UYARI: Dörtgen olmayan şekiller nedeniyle veya "
                                + "ölçeklendirme nedeniyle alan hesabında farklar oluşabilir!";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 200;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Aydınlat Komutu");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            BackButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void SymbolCommandPage(PaletteSet newPalette)
        {
            ClosePalette(newPalette);

            // Create a user control to hold the message and button
            var messageControl = new WinForms.UserControl();
            messageControl.BackColor = System.Drawing.Color.White;
            messageControl.AutoScroll = true;

            var messageBody = new WinForms.RichTextBox();
            messageBody.Text = "\n" + "   Kütüphane içeriğinde çeşitli elektrik "
                                + "ve aydınlatma sembolleri mevcuttur. "
                                + "Bu sembollerin komutları sembollerin "
                                + "adları ile aynıdır."
                                + "\n\n   Çizdirmek istediğiniz sembolün "
                                + "komutunu kullanıldığında kullanıcıdan "
                                + "sembolün çizileceği noktayı seçmesi istenecektir ve "
                                + "seçimin ardından sembol çizimi gerçekleştirilecektir. "
                                + "Bazı semboller \"R\" veya \"E\" tuşları ile döndürülebilir."
                                + "\n\n   Sembol listesi aşağıdaki gibidir:"
                                + "\n     * Anahtar"
                                + "\n     * Aplik"
                                + "\n     * Armatür"
                                + "\n     * Avize"
                                + "\n     * Buat"
                                + "\n     * Komütatör"
                                + "\n     * Priz";
            messageBody.Dock = WinForms.DockStyle.Top;
            messageBody.ReadOnly = true;
            messageBody.ScrollBars = WinForms.RichTextBoxScrollBars.Vertical;
            messageBody.WordWrap = true;
            messageBody.Height = 200;
            messageControl.Controls.Add(messageBody);

            // Create a new palette and add the message control
            newPalette = new PaletteSet("Sembol Komutları");
            newPalette.Add("LS", messageControl);
            newPalette.Size = new Size(200, 150);

            BackButton(messageControl, newPalette);
            HomeButton(messageControl, newPalette);
            CloseButton(messageControl, newPalette);

            // Display the new palette
            newPalette.Visible = true;
        }

        private void HomeButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();        // Create button
            buttonLS.Text = "Ana Menü";                  // Button name
            buttonLS.Dock = WinForms.DockStyle.Bottom;   // Button will be at bottom
            buttonLS.Click += (sender, e) =>             // Button click event
            {
                // When the button is clicked it will call HomePage() Method.
                HomePage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);   // Add the button to the usercontrol.
        }

        private void LSButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Hat ve Sembol Nedir?";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                LSPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void CommandsButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Komutlar";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                CommandsPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }
        
        private void LinyeButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Linye Komutu";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                LinyeCommandPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void SortiButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Sorti Komutu";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                SortiCommandPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void EnlightButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Aydınlatma Komutu";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                EnlightCommandPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void SymbolButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Sembol Komutları";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                SymbolCommandPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void BackButton(UserControl messageControl, PaletteSet paletteSet)
        {
            var buttonLS = new WinForms.Button();
            buttonLS.Text = "Geri";
            buttonLS.Dock = WinForms.DockStyle.Bottom;
            buttonLS.Click += (sender, e) =>
            {
                CommandsPage(paletteSet);
            };
            messageControl.Controls.Add(buttonLS);
        }

        private void CloseButton(UserControl messageControl, PaletteSet paletteSet)
        {

            var buttonClose = new WinForms.Button();
            buttonClose.Text = "Kapat";
            buttonClose.Dock = WinForms.DockStyle.Bottom;
            buttonClose.Click += (sender, e) =>
            {
                ClosePalette(paletteSet);
            };
            messageControl.Controls.Add(buttonClose);
        }

        private void ClosePalette(PaletteSet paletteSet)
        {
            if (paletteSet != null)
            {
                paletteSet.Visible = false;   // Make the palette invisible.
                paletteSet.Close();           // Close the palette.
                paletteSet.Dispose();         // Ensure cleanup.
                _ps = null;                   // Ensure again.
            }
        }

    }

}