using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Crm.Sdk.Samples;
using Microsoft.Crm.Services.Utility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Query;

namespace PatNet.Lib
{
    public class PatnetCRMHelper
    {


        public void NotifyCRM(string CRMServerName, string orgName, string shipmentNumber)
        {
            IOrganizationService _service;
            OrganizationServiceProxy _serviceProxy;
            ServerConnection serverConnect = new ServerConnection();
            ServerConnection.Configuration config =
                serverConnect.GetServerConfigurationNew(CRMServerName, orgName, "username",
                    "password", false);

            using (_serviceProxy = ServerConnection.GetOrganizationProxy(config))

            {
                _serviceProxy.ServiceConfiguration.CurrentServiceEndpoint.Behaviors.Add(new ProxyTypesBehavior());
                _serviceProxy.EnableProxyTypes();
                _service = (IOrganizationService) _serviceProxy;
 
                Entity shipment = new Entity();
                shipment.LogicalName = "unicor_shipment";
                shipment.Attributes["unicor_name"] = shipmentNumber;                
                _service.Create(shipment);

 
            }
        }

//        public void CheckStatus()
//        {
//            string fetchXml = string.Format(@"
//
//                        <fetch mapping='logical' count='1'>
//
//                           <entity name='unicor_patent'>
//
//                              <all-attributes />
//
//                              <filter type='and'>
//
//                                 <condition attribute='unicor_patentstatus' operator='eq' value='{0}' />
//
//                              </filter>
//
//                           </entity>
//
//                        </fetch>
//
//                        ", 456080003);

//            //Patent Status is Completed: 456080003

//            IOrganizationService service;

//            List<Entity> patents = ((EntityCollection)service.RetrieveMultiple(new FetchExpression(fetchXml))).Entities.ToList();

//            foreach (Entity patent in patents)
//            {

//                //You can use the relative URL or the SharePoint ID to access the patent folder.  If you would like to loop through the patent files in CRM I can send that as well

//                if (patent.Attributes.Contains("unicor_sp_relativeurl"))
//                {

//                    //Go to Sharepoint folder and get the docs

//                    string test = (string)patent.Attributes["unicor_sp_relativeurl"];

//                }

//                //OR You can use the ID instead of the URL, but you don't need both

//                if (patent.Attributes.Contains("unicor_sp_id"))
//                {

//                    //Go to Sharepoint folder

//                }



//                //He doesn't know what the Delivered options are for so for now I'm using that to assume that's when they've been put on the server

//                //Delivered w/out Error: 456080007

//                //Delivered w/Error: 456080005



//                //This will call the update method.  We're instantiating a new Entity because the system will try to update whatever fields are in the object so I tend to only include the fields I want to update

//                Entity pt = new Entity();

//                pt.LogicalName = "unicor_patent";

//                pt.Attributes["unicor_patentstatus"] = new OptionSetValue(456080007); //to set the status to delivered with/out error

//                service.Update(pt);

//            }
//        }


    }
}
