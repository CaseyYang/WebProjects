<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="WebTime.aspx.cs" Inherits="WebTime.WebForm1" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    
    <asp:Label ID="promptLabel" runat="server" Font-Bold="True" 
        Font-Size="XX-Large" Text="A Simple Web Form Example"></asp:Label>
    <p>
        <asp:Label ID="timeLabel" runat="server" ForeColor="#CC00FF" Font-Bold="True" 
            Font-Size="XX-Large" BackColor="#99FF66"></asp:Label>
    </p>
    
    </form>
</body>
</html>
