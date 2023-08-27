using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.ApplicationServices.Core;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Text;
using System.Threading.Tasks;


namespace TestLMVExtractor
{
    public class ExtractorCommands
    {
        private ObjectId solidId;
        private ObjectId layoutId;        
        static readonly string RegAppName = "CARBON_NEGATIVE";
        [CommandMethod("EXTRACTDATA")]
        public void ExtractData()
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var db = doc.Database;
            var ed = doc.Editor;          
            long handle = Convert.ToInt64("14A37", 16);
            Handle h = new Handle(handle);
            //1. Fetch House wall solid from handle.

            if (!db.TryGetObjectId(h, out solidId)) {
                ed.WriteMessage($"\nEntity Not Found for given{h.Value}");
                return; 
            }

            using (Transaction t = db.TransactionManager.StartTransaction())
            {
                var bt = (BlockTable)t.GetObject(db.BlockTableId, OpenMode.ForRead, false);
                var btr = (BlockTableRecord)t.GetObject(bt[BlockTableRecord.ModelSpace], OpenMode.ForWrite, false);
                layoutId = btr.LayoutId;
                var ent = t.GetObject(solidId, OpenMode.ForWrite) as Solid3d;
                RegAppTable tbl = (RegAppTable)t.GetObject(db.RegAppTableId, OpenMode.ForWrite, false);
                if (!tbl.Has(RegAppName))
                {
                    RegAppTableRecord app = new RegAppTableRecord
                    {
                        Name = RegAppName
                    };
                    tbl.Add(app);
                    t.AddNewlyCreatedDBObject(app, true);
                }      
                if(ent.GetXDataForApplication(RegAppName) == null)
                {
                    ent.XData = new ResultBuffer(
                    new TypedValue((int)DxfCode.ExtendedDataRegAppName, RegAppName),
                    new TypedValue((int)DxfCode.ExtendedDataAsciiString, "Using 40% fly ash – fine glass powder made primarily of iron, silica, and alumina – helps cut embodied carbon in conventional bricks.")
                    );
                   
                }
                t.Commit();
            }

            // 2. Load LMVExport.crx
            ed.Command("_ARX", "_L", "ACLMVEXPORT");

            // 3. Hook up the events           
            Application.EndExtraction += Application_EndExtraction;           
            // 4. Call LMVEXPORT command
            var folder = Directory.GetCurrentDirectory();

            ed.Command("_LMVEXPORT", folder, Path.Combine(folder, "filter.json"));          
            Application.EndExtraction -= Application_EndExtraction;

        }

    

        private void Application_EndExtraction(object sender, EndExtractionEventArgs e)
        {
            var doc = Application.DocumentManager.MdiActiveDocument;
            var ed = doc.Editor;
            var db = doc.Database;
            ed.WriteMessage("---Extraction End----");
            using (Transaction t = db.TransactionManager.StartTransaction())
            {
                var ent = t.GetObject(this.solidId, OpenMode.ForRead);
                var xdata = ent.GetXDataForApplication(RegAppName);
                if (xdata == null) return;
                
                //add xdata property
                var data = xdata.AsArray();
                e.AddProperty(solidId, "CarbonNegative", "Reason", data[1].Value, "", false);

                //add some other sustainability properties
                e.AddProperty(solidId, "CarbonNegative", "BrickType", "CarbiCrete", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Density (kg/m3)", "2250 CMU", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Water Absorption(%)", "6.0", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Compressive Strength (MPa)", "26", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Moisture(%)", "1.5", "", false);
                e.AddProperty(solidId, "CarbonNegative", "Fire Rating(hrs)", "2", "", false);
                t.Commit();
            }

        }
       
    }
}


