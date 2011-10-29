<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="_SignalTrace.aspx.cs" Inherits="Chat._SignalTrace" %>

<%@ Import Namespace="Chat" %>
<%@ Import Namespace="DynamicLINQ" %>
<script runat="server">
    protected void Page_Load(object sender, EventArgs e) {
        Logs.DataSource = Filter(TraceHelper.Logs.Keys.AsQueryable())
                                            .OrderByDescending(p => p.When)
                                            .Take(100)
                                            .OrderBy(p => p.When);
        Logs.DataBind();
    }

    private IEnumerable<LogInfo> Normalize(IEnumerable<LogInfo> logs) {
        return from l in logs
               group l by new { l.Message, l.ThreadId } into g
               let i = g.Last()
               let count = g.Count()
               select new LogInfo {
                   Category = i.Category,
                   ClientId = i.ClientId,
                   Message = i.Message + (count > 1 ? " (aggregated " + g.Count() + ")" : ""),
                   Path = i.Path,
                   When = i.When,
                   ThreadId = i.ThreadId,
                   Signal = String.Join(" ", g.Select(log => log.Signal)),
                   TaskId = i.TaskId
               };
    }

    private IQueryable<LogInfo> Filter(IQueryable<LogInfo> logs) {
        string[] filterProps = new[] { "Signal", "TaskId", "ThreadId", "Category", "ClientId" };

        foreach (var item in filterProps) {
            string value = Request.QueryString[item];
            if (!String.IsNullOrEmpty(value)) {
                logs = logs.DynamicWhere(i => i[item] == value);
            }
        }

        return logs;
    }

    public void ResetLog(object sender, EventArgs e) {
        TraceHelper.Logs.Clear();
        Response.Redirect(Request.RawUrl);
    }

</script>
<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <asp:Button Text="Clear Logs" runat="server" OnClick="ResetLog" />
        <asp:GridView ID="Logs" runat="server" AutoGenerateColumns="false" CellPadding="4">
            <Columns>
                <asp:HyperLinkField DataNavigateUrlFormatString="?Category={0}" DataNavigateUrlFields="Category"
                    DataTextField="Category" HeaderText="Category" />
                <asp:BoundField DataField="Message" HeaderText="Message" />
                <asp:HyperLinkField DataNavigateUrlFormatString="?Signal={0}" DataNavigateUrlFields="Signal"
                    DataTextField="Signal" HeaderText="Signal(s)" />
                <asp:BoundField DataField="ThreadId" HeaderText="ThreadId" />
                <asp:BoundField DataField="TaskId" HeaderText="TaskId" />
                <asp:BoundField DataField="When" HeaderText="When" />
                <asp:BoundField DataField="Path" HeaderText="Path" />
                <asp:HyperLinkField DataNavigateUrlFormatString="?ClientId={0}" DataNavigateUrlFields="ClientId"
                    DataTextField="ClientId" HeaderText="Client Id" />
            </Columns>
        </asp:GridView>
    </div>
    </form>
</body>
</html>
