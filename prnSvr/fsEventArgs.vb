Imports System.IO
Imports System.Threading

Public Class fsEventArgs : Inherits System.IO.FileSystemEventArgs

#Region "Constructor"

    Private _svc As prnSvr
    Sub New(ByRef svc As prnSvr, ByVal arg As System.IO.FileSystemEventArgs)        
        MyBase.New(arg.ChangeType, Replace(arg.FullPath, arg.Name, ""), arg.Name)
        _svc = svc

    End Sub

#End Region

#Region "Public Properties"

    Public ReadOnly Property ConvertProcessInfo() As ProcessStartInfo
        Get
            Dim ret As New ProcessStartInfo
            With ret
                .FileName = Path.Combine(_svc.exepath.FullName, "gswin32c.exe")
                .Arguments = String.Format( _
                    "-dNOPAUSE -dBATCH -sDEVICE=pxlmono -sPAPERSIZE=a4 -dFIXEDMEDIA -dPDFFitPage " & _
                    "-sOutputFile={2}{1}{2} " & _
                    "-c {2}<</BeginPage{3}0.9 0.9 scale 29.75 42.1 translate{4}>> setpagedevice{2} " & _
                    "-f {2}{0}{2}", _
                    Me.FullPath, _
                    pxl.FullName, _
                    Chr(34), _
                    "{", _
                    "}" _
                )
                _svc.Log("{0} {1}", .FileName, .Arguments)

            End With

            Return ret
        End Get
    End Property

    Public ReadOnly Property FileExt() As String
        Get
            Return New FileInfo(FullPath).Extension.ToLower
        End Get
    End Property

    Public ReadOnly Property PrinterName() As String
        Get
            Return String.Format( _
            "\\{0}\{1}", _
            Environment.MachineName, _
            Split(Name, "\")(0) _
        )
        End Get
    End Property

    Public ReadOnly Property pxl() As FileInfo
        Get
            Return New FileInfo(Replace(FullPath, ".pdf", ".pxl", , , CompareMethod.Binary))
        End Get
    End Property

#End Region

#Region "Public Methods"

    Public Sub DeleteAndWait()
        With New FileInfo(FullPath)
            While .Exists
                Try
                    _svc.Log("Deleting {0}.", .FullName)
                    .Delete()

                Catch ex As Exception
                    _svc.Log(ex.Message)
                    Thread.Sleep(500)

                End Try
                .Refresh()

            End While
        End With

    End Sub

    Public Sub WaitWrite()

        ' Wait complete
        While Not _svc.complete.Contains(FullPath)
            Thread.Sleep(100)
        End While

        ' Remove
        _svc.complete.Remove(FullPath)

    End Sub


#End Region

End Class
