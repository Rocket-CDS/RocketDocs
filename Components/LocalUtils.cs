using DNNrocketAPI.Components;
using Simplisity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RocketDocs.Components
{
    public static class LocalUtils
    {
        public const string ControlPath = "/DesktopModules/DNNrocketModules/RocketDocs";
        public const string ResourcePath = "/DesktopModules/DNNrocketModules/RocketDocs/App_LocalResources";

        /// <summary>
        /// Get a resouerce string from a resx file in "/DesktopModules/DNNrocketModules/RocketEcommerce/App_LocalResources" 
        /// </summary>
        /// <param name="resourceKey">[filename].[resourcekey]</param>
        /// <param name="resourceExt">[resource key extention]</param>
        /// <param name="cultureCode">[culturecode to fetch]</param>
        /// <returns></returns>
        public static string ResourceKey(string resourceKey, string resourceExt = "Text", string cultureCode = "")
        {
            return DNNrocketUtils.GetResourceString(ResourcePath, resourceKey, resourceExt, cultureCode);
        }
        
        public static string TokenReplacementCultureCode(string str, string CultureCode)
        {
            if (CultureCode == "") return str;
            str = str.Replace("{culturecode}", CultureCode);
            var s = CultureCode.Split('-');
            if (s.Count() == 2)
            {
                str = str.Replace("{language}", s[0]);
                str = str.Replace("{country}", s[1]);
            }
            return str;
        }
    }

}
