using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using Microsoft.SharePoint;
using Microsoft.SharePoint.Security;
using Microsoft.SharePoint.Administration.Claims;


namespace DepartmentClaimsProvider.Features.Feature1
{
    /// <summary>
    /// This class handles events raised during feature activation, deactivation, installation, uninstallation, and upgrade.
    /// </summary>
    /// <remarks>
    /// The GUID attached to this class may be used during packaging and should not be modified.
    /// </remarks>

    [Guid("814d81e1-6d71-49fb-8619-6473b1632f09")]
    public class Feature1EventReceiver : SPClaimProviderFeatureReceiver
    {
        // Uncomment the method below to handle the event raised after a feature has been activated.

        public override void FeatureActivated(SPFeatureReceiverProperties properties)
        {
            base.FeatureActivated(properties);
        }


        // Uncomment the method below to handle the event raised before a feature is deactivated.

        public override void FeatureDeactivating(SPFeatureReceiverProperties properties)
        {
            base.FeatureDeactivating(properties);
        }


        // Uncomment the method below to handle the event raised after a feature has been installed.

        //public override void FeatureInstalled(SPFeatureReceiverProperties properties)
        //{
        //}


        // Uncomment the method below to handle the event raised before a feature is uninstalled.

        //public override void FeatureUninstalling(SPFeatureReceiverProperties properties)
        //{
        //}

        // Uncomment the method below to handle the event raised when a feature is upgrading.

        //public override void FeatureUpgrading(SPFeatureReceiverProperties properties, string upgradeActionName, System.Collections.Generic.IDictionary<string, string> parameters)
        //{
        //}
        public override string ClaimProviderAssembly
        {
            get { return typeof(AdDepartmentClaimsProvider.DepartmentClaimsProvider).Assembly.FullName; }
        }

        public override string ClaimProviderDescription
        {
            get { return "Claims provider for Department field from AD."; }
        }

        public override string ClaimProviderDisplayName
        {
            get { return AdDepartmentClaimsProvider.DepartmentClaimsProvider.ProviderDisplayName; }
        }

        public override string ClaimProviderType
        {
            get { return typeof(AdDepartmentClaimsProvider.DepartmentClaimsProvider).FullName; }
        }
    }
}
