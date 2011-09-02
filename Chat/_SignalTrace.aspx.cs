using System;
using System.Linq;
using DynamicLINQ;

namespace Chat {
    public partial class _SignalTrace : System.Web.UI.Page {
        protected void Page_Load(object sender, EventArgs e) {
            Logs.DataSource = Filter(TraceHelper.Logs.AsQueryable()).OrderByDescending(l => l.When)
                                              .Take(100)
                                              .Reverse();
            Logs.DataBind();
        }

        private IQueryable<LogInfo> Filter(IQueryable<LogInfo> items) {
            string[] filterProps = new[] { "Signal", "TaskId", "ThreadId", "Category", "ClientId" };

            foreach (var item in filterProps) {
                string value = Request.QueryString[item];
                if (!String.IsNullOrEmpty(value)) {
                    items = items.DynamicWhere(i => i[item] == value);
                }
            }

            return items;
        }
    }
}