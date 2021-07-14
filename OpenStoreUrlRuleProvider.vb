Imports Microsoft.VisualBasic
Imports Satrabel.HttpModules.Provider
Imports DotNetNuke.Entities.Portals
Imports System.Data.SqlClient
Imports DotNetNuke.Entities.Modules

Public Class OpenStoreUrlRuleProvider
    Inherits UrlRuleProvider

    Public Overrides Function GetRules(PortalId As Integer) As List(Of UrlRule)

        Dim portal As PortalInfo = (New PortalController).GetPortal(PortalId)
        Dim rules As New List(Of UrlRule)
        Dim dicSecondaryLocales As List(Of Locale) = LocaleController.Instance.GetLocales(PortalId).Values.Where(Function(l) l.Code <> portal.DefaultLanguage).ToList

        Dim Result As OSUrlDetail
        'Dim conString As String = ConfigurationManager.ConnectionStrings("SiteSqlServer").ConnectionString
        Dim conString As String = DotNetNuke.Common.Utilities.Config.GetConnectionString()

        Using connection As SqlConnection = New SqlConnection(conString)
            If connection.State = ConnectionState.Closed Then
                connection.Open()
            End If

            Using sqlCommand As SqlCommand = New SqlCommand("OS_GetDetailsForRewiteUrl", connection)
                sqlCommand.CommandType = System.Data.CommandType.StoredProcedure

                Using sqlDataReader As SqlDataReader = sqlCommand.ExecuteReader()
                    If (sqlDataReader.HasRows) Then
                        While (sqlDataReader.Read())
                            Result = New OSUrlDetail()
                            Result.TabId = sqlDataReader.GetInt32(0)
                            Result.CategoryID = sqlDataReader.GetInt32(1)
                            Result.CategoryName = sqlDataReader.GetString(2)
                            Result.ProductID = sqlDataReader.GetInt32(3)
                            Result.ProductName = sqlDataReader.GetString(4)
                            Result.Url = UrlRuleProvider.CleanupUrl(sqlDataReader.GetString(5))
                            Result.Parameters = sqlDataReader.GetString(6)

                            If Result.CategoryID > 0 And Result.ProductID = 0 Then
                                Result.RedirectDestination = Result.Parameters.Replace("=", "/").Replace("&", "/") + "/" + CleanupSEO(Result.CategoryName)
                                Result.RedirectDestination = Result.RedirectDestination.ToLower()
                                rules.Add(New UrlRule With {.Action = UrlRuleAction.Rewrite, .Parameters = Result.Parameters, .RuleType = UrlRuleType.Module, .Url = Cleanup(Result.Url), .TabId = Result.TabId, .RemoveTab = False, .RedirectDestination = Result.RedirectDestination})
                            ElseIf Result.CategoryID > 0 And Result.ProductID > 0 Then
                                rules.Add(New UrlRule With {.Action = UrlRuleAction.Rewrite, .Parameters = Result.Parameters, .RuleType = UrlRuleType.Module, .Url = Cleanup(Result.Url), .TabId = Result.TabId, .RemoveTab = False})
                            End If

                        End While
                    End If
                    sqlDataReader.Close()
                End Using
            End Using
        End Using

        Return rules

    End Function

    Private Function CleanupSEO(ByVal SEOName As String) As String
        If (Not String.IsNullOrEmpty(SEOName)) Then
            SEOName = SEOName.Replace(" ", "_")
            SEOName = Regex.Replace(SEOName, "[\W]", "")
        End If
        Return SEOName
    End Function

    Private Function Cleanup(ByVal SEOName As String) As String
        If (Not String.IsNullOrEmpty(SEOName)) Then
            SEOName = SEOName.Replace("_", "/")
        End If
        Return SEOName
    End Function
End Class

Public Class OSUrlDetail
    Public TabId As Integer
    Public CategoryID As Integer
    Public CategoryName As String
    Public ProductID As Integer
    Public ProductName As String
    Public Url As String
    Public Parameters As String
    Public RedirectDestination As String
End Class