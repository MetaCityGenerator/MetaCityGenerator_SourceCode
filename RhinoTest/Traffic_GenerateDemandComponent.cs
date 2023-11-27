using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Timers;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Geometry;
using MetaCity.Planning.Sustainability;
using System.Threading;
using Eto.Threading;
using Thread = System.Threading.Thread;
using System.Text;
using System.Threading.Tasks;
using MetaCity.Traffic;
using MetaCityGenerator.Properties;

namespace MetaCityGenerator
{
    public class Traffic_GenerateDemandComponent : GH_Component
    {
        private string latestOutput = "";
        private Process process;
        /// <summary>
        /// Initializes a new instance of the Traffic_GenerateDemandComponent class.
        /// </summary>
        public Traffic_GenerateDemandComponent()
          : base("Traffic_GenerateDemand", "TR_GD",
                "Generate MATSIM Traffic Demand",
              "MetaCity", "8_Traffic")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item);
            pManager.AddTextParameter("InputDir", "In", "This is the input directory path, make sure building.geojson and network.geojson are included.", GH_ParamAccess.item);
            pManager.AddTextParameter("OutputDir", "Out", "This is the output directory path", GH_ParamAccess.item);
            //pManager.AddTextParameter("IndexDir", "Index", "This is the path of the index file", GH_ParamAccess.item);
        }

        /// <summary>
        /// Registers all the output parameters for this component.
        /// </summary>
        protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
        {
            pManager.AddTextParameter("GeneratedResult", "Res", "GenerateResult", GH_ParamAccess.item);
        }

        /// <summary>
        /// This is the method that actually does the work.
        /// </summary>
        /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
        protected override void SolveInstance(IGH_DataAccess DA)
        {   
            string inputPath = "";
            string outputPath = "";
            string indexPath = "";
            bool run = false;
            if (!DA.GetData(1, ref inputPath) || !DA.GetData(2, ref outputPath) || !DA.GetData(0, ref run))
            {
                return;
            }

            var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            indexPath = Path.Combine(defaultPath, "data", "indexCalculation.xml");

            string jarPath = Path.Combine(defaultPath, "matsim_preparation_jar", "generate_demand", "matsim_preparation.jar");
            if (run)
            {
                Thread thread = new Thread(() =>
                {
                    process = new Process();
                    process.StartInfo.FileName = "java";
                    process.StartInfo.Arguments = $"-jar {jarPath} {inputPath+ Path.DirectorySeparatorChar} {outputPath+ Path.DirectorySeparatorChar} {indexPath}"; ;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;

                    UOutputForm form = new UOutputForm(process, 500, 300);

                    process.OutputDataReceived += (s, eventData) =>
                    {
                        if (!string.IsNullOrEmpty(eventData.Data) && !form.IsDisposed && form.InvokeRequired)
                        {
                            form.Invoke(new Action(() => form.OutputTextBox.AppendText(eventData.Data + Environment.NewLine)));
                        }
                    };

                    process.ErrorDataReceived += (s, eventData) =>
                    {
                        if (!string.IsNullOrEmpty(eventData.Data) && !form.IsDisposed && form.InvokeRequired)
                        {
                            form.Invoke(new Action(() => form.OutputTextBox.AppendText("[ERROR] " + eventData.Data + Environment.NewLine)));
                        }
                    };

                    try
                    {
                        process.Start();
                        process.BeginOutputReadLine();
                        process.BeginErrorReadLine();
                    }
                    catch (Exception ex)
                    {
                        if(!form.IsDisposed && form.InvokeRequired)
                        {
                            form.Invoke(new Action(() => form.OutputTextBox.AppendText("[EXCEPTION] " + ex.Message + Environment.NewLine)));
                        }
                    }

                    Application.Run(form);
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            DA.SetData(0, outputPath);
        }

        public static string RunJavaJar(string jarPath, string inputPath, string outputPath, string indexPath)
        {
            Process process = new Process();

            process.StartInfo.FileName = "java";
            process.StartInfo.Arguments = $"-jar {jarPath} {inputPath} {outputPath} {indexPath}"; // 将输入和输出路径作为参数传递给.jar文件
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;

            process.Start();

            // 读取Java程序的输出
            string output = process.StandardOutput.ReadToEnd();

            process.WaitForExit();
            return output;
        }

        

        /// <summary>
        /// Provides an Icon for the component.
        /// </summary>
        protected override System.Drawing.Bitmap Icon
        {
            get
            {
                //You can add image files to your project resources and access them like this:
                // return Resources.IconForThisComponent;
                return Resources.TR_GenerateDemand;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("4E20189D-08AA-46E0-B100-F125790CD402"); }
        }
    }
}