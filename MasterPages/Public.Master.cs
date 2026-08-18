using System;
using System.Web.UI;

namespace CRMP.MasterPages
{
    public partial class PublicMaster : MasterPage
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            // Public master page - no authentication required
        }
    }
}
