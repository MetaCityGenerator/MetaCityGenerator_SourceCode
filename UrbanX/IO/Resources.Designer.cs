﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace UrbanX.IO.Properties
{
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Resources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Resources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("UrbanX.IO.Resources", typeof(Resources).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to &quot;coordinates&quot; array is not compatible with &quot;type&quot; value of &quot;{0}&quot;.
        /// </summary>
        internal static string EX_CoordinatesIncompatibleWithType {
            get {
                return ResourceManager.GetString("EX_CoordinatesIncompatibleWithType", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected token is &apos;]&apos; but was &apos;{0}&apos;..
        /// </summary>
        internal static string EX_EndArrayTokenExpected {
            get {
                return ResourceManager.GetString("EX_EndArrayTokenExpected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No geometries defined to build a &apos;GeometryCollection&apos;..
        /// </summary>
        internal static string EX_GCTypeWithoutGeometries {
            get {
                return ResourceManager.GetString("EX_GCTypeWithoutGeometries", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to No coordinates defined for geometry of type &apos;{0}&apos;.
        /// </summary>
        internal static string EX_NoCoordinatesDefined {
            get {
                return ResourceManager.GetString("EX_NoCoordinatesDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Missing &quot;type&quot; property.
        /// </summary>
        internal static string EX_NoGeometryTypeDefined {
            get {
                return ResourceManager.GetString("EX_NoGeometryTypeDefined", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected token is &apos;[&apos; but was &apos;{0}&apos;..
        /// </summary>
        internal static string EX_StartArrayTokenExpected {
            get {
                return ResourceManager.GetString("EX_StartArrayTokenExpected", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Stream ended unexpectedly..
        /// </summary>
        internal static string EX_UnexpectedEndOfStream {
            get {
                return ResourceManager.GetString("EX_UnexpectedEndOfStream", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected token is &apos;{0}&apos; but was &apos;{1}&apos; (Value &apos;{2}&apos;)..
        /// </summary>
        internal static string EX_UnexpectedToken {
            get {
                return ResourceManager.GetString("EX_UnexpectedToken", resourceCulture);
            }
        }
    }
}
