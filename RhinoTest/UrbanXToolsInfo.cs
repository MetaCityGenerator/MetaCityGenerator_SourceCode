using Grasshopper.Kernel;

using System;
using System.Drawing;

namespace MetaCityGenerator
{
    public class MetaCityGeneratorInfo : GH_AssemblyInfo
    {
        public override string Name
        {
            get
            {
                return "MetaCityGenerator";
            }
        }
        public override Bitmap Icon
        {
            get
            {
                //Return a 24x24 pixel bitmap to represent this GHA library.
                return null;
            }
        }
        public override string Description
        {
            get
            {
                //Return a short string describing the purpose of this GHA library.
                return "MetaCityGenerator GH plugin for urban designers";
            }
        }

        public override string AssemblyVersion
        {
            get
            {
                return "v.0.9.0_" + DateTime.Now.ToShortDateString();
            }
        }


        public override Guid Id
        {
            get
            {
                return new Guid("0c7a34a8-d79a-411a-a351-2d19fb56f159");
            }
        }

        public override string AuthorName
        {
            get
            {
                //Return a string identifying you or your company.
                return "WLuo, LinAlh, Deng, Yang";
            }
        }
        public override string AuthorContact
        {
            get
            {
                //Return a string representing your preferred contact details.
                return "MetaCitylab.net";
            }
        }
    }
}
