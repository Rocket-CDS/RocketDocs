using System;
using System.Collections.Generic;
using System.Text;
using DNNrocketAPI;
using DNNrocketAPI.Components;
using RocketDocs.Components;
using Simplisity;

namespace RocketDocs.Components
{
    public class Scheduler : SchedulerInterface
    {
        /// <summary>
        /// This is called by DNNrocketAPI.Components.RocketScheduler
        /// </summary>
        /// <param name="systemData"></param>
        /// <param name="rocketInterface"></param>
        public override void DoWork()
        {
            var portalList = PortalUtils.GetPortals();
            foreach (var portalId in portalList)
            {
                var portalContent = new PortalContentLimpet(portalId, DNNrocketUtils.GetCurrentCulture());
                if (portalContent.Active)
                {

                }
            }
        }
    }
}