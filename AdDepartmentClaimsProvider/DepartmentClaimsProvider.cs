using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.SharePoint;
using Microsoft.IdentityModel;
using Microsoft.SharePoint.Administration.Claims;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices;
using Microsoft.SharePoint.WebControls;
using Microsoft.SharePoint.Administration;

namespace AdDepartmentClaimsProvider
{
    public class DepartmentClaimsProvider : SPClaimProvider
    {
        string CompanyPropertyName = "company";
        string DepartmentPropertyName = "department";
        private static string DepartmentClaimType
        {
            get
            {
                return "http://schema.hpc2.local/departments";
            }
        }

        private static string DepartmentClaimValueType
        {
            get
            {
                return Microsoft.IdentityModel.Claims.ClaimValueTypes.String;
            }
        }

        public DepartmentClaimsProvider(string displayName)
            : base(displayName)
        {

        }

        protected override void FillClaimTypes(List<string> claimTypes)
        {
            if (claimTypes == null)
                throw new ArgumentNullException("claimTypes");

            // Add our claim type.
            claimTypes.Add(DepartmentClaimType);
        }

        protected override void FillClaimValueTypes(List<string> claimValueTypes)
        {
            if (claimValueTypes == null)
                throw new ArgumentNullException("claimValueTypes");

            // Add our claim type.
            claimValueTypes.Add(DepartmentClaimValueType);
        }

        protected override void FillClaimsForEntity(Uri context, SPClaim entity, List<SPClaim> claims)
        {


            if (entity == null)
            {
                Logger.LogError("entity is null");
                throw new ArgumentNullException("entity");
            }

            if (claims == null)
            {
                Logger.LogError("claims is null");
                throw new ArgumentNullException("claims");
            }

            // Determine who the user is, so that we know what team to add to their claim
            // entity. The value from the input parameter contains the name of the 
            // authenticated user. For a SQL forms-based authentication user, it looks similar to
            // 0#.f|sqlmembership|user1; for a Windows claims user it looks similar 
            // to 0#.w|steve\\wilmaf.
            // I will skip some uninteresting code here, to look at that name and determine
            // whether it is a forms-based authentication user or a Windows user, and if it is a forms-based authentication user, 
            // determine what the number part of the name is that follows "user".

            string department = string.Empty;
            string userName = string.Empty;
            //if (entity.ClaimType == SPOriginalIssuerType.Windows
            string[] entityDetails = entity.Value.Split('|');
            switch (entityDetails[0])
            {
                case "0#.f": userName = entityDetails[2]; break;
                case "0#.w": userName = entityDetails[1].Split('\\')[1]; break;
            }
            SearchResult user = GetFilteredEntity(String.Format("(SAMAccountName={0})", userName));
            if (user != null)
            {
                department = GetClaimFromEntity(user);
                //if ((user.Properties[CompanyPropertyName].Count > 0) && (user.Properties[DepartmentPropertyName].Count > 0))
                //{
                //    department = GetClaimFromEntity(user);//user.Properties[DepartmentPropertyName][0].ToString();
                //}
            }
            Logger.LogInfo(string.Format("Assigned {0} claim for {1}",department, userName));
            // After the uninteresting code, "userID" will equal -1 if it is a Windows user.
            // If it is a forms-based authentication user, it will contain the number that follows "user".

            // Determine what the user's favorite team is.
            //if (userID > 0)
            //{
            //    // Plug in the appropriate team.
            //    if (userID > 30)
            //        department = ourTeams[2];
            //    else if (userID > 15)
            //        department = ourTeams[1];
            //    else
            //        department = ourTeams[0];
            //}
            //else
            //    department = ourTeams[1];
            // If they are not one of our forms-based authentication users, 
            // make their favorite team Wingtip Toys.

            // Add the claim.
            claims.Add(CreateClaim(DepartmentClaimType, department, DepartmentClaimValueType));
        }

        protected override void FillEntityTypes(List<string> entityTypes)
        {
            entityTypes.Add(SPClaimEntityTypes.FormsRole);
        }

        protected override void FillHierarchy(Uri context, string[] entityTypes, string hierarchyNodeID, int numberOfLevels, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree hierarchy)
        {
            // Ensure that People Picker is asking for the type of entity that we 
            // return; site collection administrator will not return, for example.
            if (!EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
                return;

            // Check to see whether the hierarchyNodeID is null; it is when the control 
            // is first loaded, but if a user clicks on one of the nodes, it will return
            // the key of the node that was clicked. This lets you build out a 
            // hierarchy as a user clicks something, instead of all at once. So I 
            // wrote the code for that scenario, but I am not using it in that way.  
            // Which means that I could have just as easily used an 
            // if (hierarchyNodeID == null) in this particular implementation, instead
            // of a switch statement.
            SPSecurity.RunWithElevatedPrivileges(() =>
                {
                    switch (hierarchyNodeID)
                    {
                        case null:
                            foreach (string node0 in GetCompanies())
                            {
                                hierarchy.AddChild(new
                                  Microsoft.SharePoint.WebControls.SPProviderHierarchyNode(
                                  DepartmentClaimsProvider.ProviderInternalName,
                                  node0,
                                  node0,
                                  true));
                            }

                            break;
                        default:
                            SearchResultCollection foundEntities = GetUserEntities(string.Format("({0}={1})", CompanyPropertyName, hierarchyNodeID));
                    if (foundEntities != null)
                    {
                        foreach (SearchResult entity in foundEntities)
                        {
                            
                            if (entity.Properties[DepartmentPropertyName].Count > 0)
                            {
                                PickerEntity pe = GetPickerEntity(GetClaimFromEntity(entity));
                                //ProviderHierarchyNode matchNode;

                                // Get the node for this team.
                                //matchNode = hierarchy.Children.Where(theNode =>
                                    //theNode.HierarchyNodeID == hierarchyNodeID).First();

                                // Add the picker entity to our tree node.
                                if (pe != null)
                                {
                                   if(hierarchy.EntityData.FirstOrDefault(p=>p.Claim.Value == pe.Claim.Value)== null) hierarchy.AddEntity(pe);
                                }
                            }
                            
                            
                        }
                    }
                            break;
                    }
                });

        }

        protected override void FillResolve(Uri context, string[] entityTypes, SPClaim resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            FillResolve(context, entityTypes, resolveInput.Value, resolved);
            
        }

        protected override void FillResolve(Uri context, string[] entityTypes, string resolveInput, List<Microsoft.SharePoint.WebControls.PickerEntity> resolved)
        {
            // Ensure that People Picker is asking for the type of entity that we 
            // return; site collection administrator will not return, for example.
            if (!EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
                return;
            //SearchResultCollection matchingEntities = null;
            SPSecurity.RunWithElevatedPrivileges(() =>
                {
                    PickerEntity pe = ResolveEntity(resolveInput);//GetPickerEntity(GetClaimFromEntity(matchingEntities[0]));
                    if (pe != null)
                    {
                        // Add it to the return list of picker entries.
                        resolved.Add(pe);
                    }
                    //matchingEntities = GetUserEntities(string.Format("({0}={1})", DepartmentPropertyName, resolveInput));
                    //if (matchingEntities.Count > 0)
                    //{
                    //    PickerEntity pe = GetPickerEntity(GetClaimFromEntity(matchingEntities[0]));

                    //    // Add it to the return list of picker entries.
                    //    resolved.Add(pe);
                    //}
                });
            
        }

        protected override void FillSchema(Microsoft.SharePoint.WebControls.SPProviderSchema schema)
        {
            // Add the schema element that we need at a minimum in our picker node.
            schema.AddSchemaElement(new
                  SPSchemaElement(PeopleEditorEntityDataKeys.DisplayName,
                  "Claim", SPSchemaElementType.Both));
        }

        protected override void FillSearch(Uri context, string[] entityTypes, string searchPattern, string hierarchyNodeID, int maxCount, Microsoft.SharePoint.WebControls.SPProviderHierarchyTree searchTree)
        {
            // Ensure that People Picker is asking for the type of entity that we 
            // return; site collection administrator will not return, for example.
            if (!EntityTypesContain(entityTypes, SPClaimEntityTypes.FormsRole))
                return;
            SearchResultCollection foundEntities = null;
            SPSecurity.RunWithElevatedPrivileges(() =>
                {
                    foundEntities = GetDirectoryEntities(searchPattern+"*");
                    if (foundEntities != null)
                    {
                        foreach (SearchResult entity in foundEntities)
                        {
                            PickerEntity pe = GetPickerEntity(GetClaimFromEntity(entity));
                            string NodeId = "Unspecified";
                            if (entity.Properties[CompanyPropertyName].Count > 0)
                            {
                                NodeId = entity.Properties[CompanyPropertyName][0].ToString();
                            }
                            // If we did not have a hierarchy, we would add it here
                            // by using the list described previously.
                            // matches.Add(pe);

                            // Add the team node where it should be displayed; 
                            // ensure that we have not already added a node to the tree
                            // for this team's location.
                            SPProviderHierarchyNode matchNode;
                            if (!searchTree.HasChild(NodeId))
                            {
                                // Create the node so that we can show our match in there too.
                                matchNode = new
                                SPProviderHierarchyNode(DepartmentClaimsProvider.ProviderInternalName,
                                NodeId,
                                NodeId,
                                true);

                                // Add it to the tree.
                                searchTree.AddChild(matchNode);
                            }
                            else
                                // Get the node for this team.
                                matchNode = searchTree.Children.Where(theNode =>
                                theNode.HierarchyNodeID == NodeId).First();

                            // Add the picker entity to our tree node.
                           if (matchNode.EntityData.FirstOrDefault(p=>p.Claim.Value == pe.Claim.Value)== null) matchNode.AddEntity(pe);
                        }
                    }
                });
            

        }

        private PickerEntity GetPickerEntity(string ClaimValue)
        {

            // Use the helper function!
            PickerEntity pe = CreatePickerEntity();

            // Set the claim that is associated with this match.
            pe.Claim = CreateClaim(DepartmentClaimType, ClaimValue, DepartmentClaimValueType);

            // Set the tooltip that is displayed when you pause over the resolved claim.
            pe.Description = DepartmentClaimsProvider.ProviderDisplayName + ":" + ClaimValue;

            // Set the text that we will display.
            pe.DisplayText = GetDisplayTextFromClaimValue(ClaimValue); //ClaimValue;

            // Store it here, in the hashtable **
            pe.EntityData[PeopleEditorEntityDataKeys.DisplayName] = ClaimValue;

            // We plug this in as a role type entity.
            pe.EntityType = SPClaimEntityTypes.FormsRole;

            // Flag the entry as being resolved.
            pe.IsResolved = true;

            // This is the first part of the description that shows
            // above the matches, like Role: Forms Auth when
            // you do an forms-based authentication search and find a matching role.
            pe.EntityGroupName = "Department";

            return pe;
        }

        string GetDisplayTextFromClaimValue(string claimValue)
        {
            return string.Format("{0} ({1})", claimValue.Split('|')[0], claimValue.Split('|')[1]);
        }

        SearchResultCollection GetFilteredEntities(string Filter)
        {
            using (Domain rootDomain = Forest.GetCurrentForest().RootDomain)
            {
                using (DirectorySearcher searcher = new DirectorySearcher(rootDomain.GetDirectoryEntry()))
                {

                    searcher.SearchScope = SearchScope.Subtree;

                    searcher.ReferralChasing = ReferralChasingOption.All;

                    searcher.PropertiesToLoad.Add(CompanyPropertyName);
                    searcher.PropertiesToLoad.Add(DepartmentPropertyName);
                    searcher.Filter = Filter;//string.Format("(&(objectClass=user)(objectCategory=person)({0}>={1}))", DepartmentPropertyName, SearchPattern);
                    SearchResultCollection result = searcher.FindAll();
                    return result;
                    //DirectoryEntry directoryEntry = result.GetDirectoryEntry();
                }
            }
        }

        SearchResult GetFilteredEntity(string Filter)
        {
            using (Domain rootDomain = Forest.GetCurrentForest().RootDomain)
            {
                using (DirectorySearcher searcher = new DirectorySearcher(rootDomain.GetDirectoryEntry()))
                {

                    searcher.SearchScope = SearchScope.Subtree;

                    searcher.ReferralChasing = ReferralChasingOption.All;

                    searcher.PropertiesToLoad.Add(CompanyPropertyName);
                    searcher.PropertiesToLoad.Add(DepartmentPropertyName);
                    searcher.Filter = Filter;//string.Format("(&(objectClass=user)(objectCategory=person)({0}>={1}))", DepartmentPropertyName, SearchPattern);
                    SearchResult result = searcher.FindOne();
                    return result;
                    //DirectoryEntry directoryEntry = result.GetDirectoryEntry();
                }
            }
        }

        IEnumerable<string> GetCompanies()
        {
            //string userName = System.Security.Principal.WindowsIdentity.GetCurrent().Name;
            List<string> companiesAvailable = new List<string>();
            foreach (SearchResult user in GetDirectoryEntities())
            {
                if (user.Properties[DepartmentPropertyName].Count > 0)
                {
                    string companyName = string.Empty;
                    if (user.Properties[CompanyPropertyName].Count == 0)
                    {
                        companyName = "Unspecified";
                    }
                    else
                    {
                        companyName = user.Properties[CompanyPropertyName][0].ToString();
                    }
                    if (!companiesAvailable.Contains(companyName))
                        companiesAvailable.Add(companyName);
                }


            }
            return companiesAvailable.OrderBy(c => c);
        }

        SearchResultCollection GetUserEntities(string UserFilter)
        {
            return GetFilteredEntities(string.Format("(&(objectClass=user)(objectCategory=person){0})", UserFilter));
        }

        SearchResult GetUserEntity(string UserFilter)
        {
            return GetFilteredEntity(string.Format("(&(objectClass=user)(objectCategory=person){0})", UserFilter));
        }

        SearchResultCollection GetDirectoryEntities(string SearchPattern)
        {
            return GetUserEntities(string.Format("({0}={1})", DepartmentPropertyName, SearchPattern));
            
        }
        SearchResultCollection GetDirectoryEntities()
        {
            return GetUserEntities(string.Empty);

        }

        string GetClaimFromEntity(SearchResult entity)
        {
            if (entity != null)
            {
                if (entity.Properties[CompanyPropertyName].Count > 0)
                {
                    if (entity.Properties[DepartmentPropertyName].Count > 0)
                    {
                        return string.Format("{0}|{1}", entity.Properties[DepartmentPropertyName][0].ToString(), entity.Properties[CompanyPropertyName][0].ToString());
                    }
                }
            }
            return string.Empty;
        }

        PickerEntity ResolveEntity(string resolveInput)
        {
            PickerEntity pe = null;//GetPickerEntity(string.Empty);
            string[] inputData = resolveInput.Split('|');
            if (inputData.Length == 2)
            {
                SearchResult matchingEntity = GetUserEntity(string.Format("({0}={1})({2}={3})",CompanyPropertyName,inputData[1], DepartmentPropertyName, inputData[0]));
                if (matchingEntity != null)
                {
                    pe = GetPickerEntity(GetClaimFromEntity(matchingEntity));
                }
            }
            //if (inputData.Length == 1)
            //{
            //    SearchResultCollection matchingEntities = GetCompanies(); GetUserEntities(string.Format("({0}={1})", DepartmentPropertyName, inputData[0]));
            //    if (matchingEntities.Count > 0)
            //    {
            //    }
            //}
            return pe;
        }

        public static string ProviderDisplayName
        {
            get
            {
                return "Department";
            }
        }


        public static string ProviderInternalName
        {
            get
            {
                return "Department";
            }
        }

        public override string Name
        {
            get { return ProviderInternalName; }
        }

        public override bool SupportsEntityInformation
        {
            get { return true; }
        }

        public override bool SupportsHierarchy
        {
            get { return true; }
        }

        public override bool SupportsResolve
        {
            get { return true; }
        }

        public override bool SupportsSearch
        {
            get { return true; }
        }
    }
}
