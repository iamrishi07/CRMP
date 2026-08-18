<%@ Page Language="C#" AutoEventWireup="true" %>
<%
    // Redirect to Dashboard. If the user is not authenticated, 
    // Forms Authentication will automatically intercept and redirect to Login.aspx.
    Response.Redirect("~/Pages/Employee/Dashboard.aspx");
%>
