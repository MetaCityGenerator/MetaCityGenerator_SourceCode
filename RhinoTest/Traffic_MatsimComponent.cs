using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;
using Grasshopper.Kernel;
using Rhino.Geometry;
using UrbanX.Traffic;
using UrbanXTools.Properties;

namespace UrbanXTools
{
    public class Traffic_MatsimComponent : GH_Component
    {
        private string latestOutput = "";
        private Process process;
        /// <summary>
        /// Initializes a new instance of the Traffic_MatsimComponent class.
        /// </summary>
        public Traffic_MatsimComponent()
          : base("Traffic_RunSim", "TR_Run",
                "Run simulation based on generated conditions",
              "UrbanX", "8_Traffic")
        {
        }

        /// <summary>
        /// Registers all the input parameters for this component.
        /// </summary>
        protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
        {
            pManager.AddBooleanParameter("Run", "Run", "Run the component", GH_ParamAccess.item);
            pManager.AddTextParameter("InputDir", "In", "This is the input directory path", GH_ParamAccess.item);
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
            bool run = false;
            if (!DA.GetData(1, ref inputPath) || !DA.GetData(0, ref run))
            {
                return;
            }

            var defaultPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string default_configPath = Path.Combine(defaultPath, "matsim_preparation_jar", "config.xml");
            string configPath = Path.Combine(inputPath, "config_tran_sim.xml");
            File.Copy(default_configPath, configPath, true);

            string jarPath = Path.Combine(defaultPath, "matsim_preparation_jar", "traffic_sim", "matsim_preparation.jar");
            string output = configPath;
            if (run)
            {
                Thread thread = new Thread(() =>
                {
                    process = new Process();
                    process.StartInfo.FileName = "java";
                    process.StartInfo.Arguments = $"-jar {jarPath} {configPath}"; ;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.StartInfo.RedirectStandardError = true;

                    UOutputForm form = new UOutputForm(process, 800, 300);

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

                    Application.Run(form);
                });

                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }

            DA.SetData(0, inputPath);
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
                return Resources.TR_Simulation;
            }
        }

        /// <summary>
        /// Gets the unique ID for this component. Do not change this ID after release.
        /// </summary>
        public override Guid ComponentGuid
        {
            get { return new Guid("8FD1658E-2C1F-4794-89CA-21E8E55C90BB"); }
        }
    }
}