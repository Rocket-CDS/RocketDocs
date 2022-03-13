using DNNrocketAPI.Components;
using RocketDocs.Components;
using Simplisity;
using System;
using System.Collections.Generic;
using System.Text;

namespace RocketDocs.API
{
    public partial class StartConnect : DNNrocketAPI.APInterface
    {
        public override Dictionary<string, object> ProcessCommand(string paramCmd, SimplisityInfo systemInfo, SimplisityInfo interfaceInfo, SimplisityInfo postInfo, SimplisityInfo paramInfo, string langRequired = "")
        {
            systemInfo.SetXmlProperty("genxml/systemkey", "rocketdocs");
            var catalogStartConnect = new RocketCatalog.API.StartConnect();
            return catalogStartConnect.ProcessCommand(paramCmd, systemInfo, interfaceInfo, postInfo, paramInfo, langRequired);
        }
    }

}
