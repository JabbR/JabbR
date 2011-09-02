<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="_SignalTrace.aspx.cs" Inherits="Chat._SignalTrace" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div> 
        <asp:GridView ID="Logs" runat="server" AutoGenerateColumns="false" CellPadding="4">
            <Columns>
                <asp:HyperLinkField DataNavigateUrlFormatString="?Category={0}" DataNavigateUrlFields="Category" DataTextField="Category" HeaderText="Category" />
                <asp:BoundField DataField="Message" HeaderText="Message" />
                <asp:HyperLinkField DataNavigateUrlFormatString="?Signal={0}" DataNavigateUrlFields="Signal" DataTextField="Signal" HeaderText="Signal" />
                <asp:BoundField DataField="ThreadId" HeaderText="ThreadId" />
                <asp:BoundField DataField="TaskId" HeaderText="TaskId" />
                <asp:BoundField DataField="When" HeaderText="When" />
                <asp:BoundField DataField="Path" HeaderText="Path" />
                <asp:HyperLinkField DataNavigateUrlFormatString="?ClientId={0}" DataNavigateUrlFields="ClientId" DataTextField="ClientId" HeaderText="Client Id" />
            </Columns>
        </asp:GridView>
    </div>
    </form>
</body>
</html>
