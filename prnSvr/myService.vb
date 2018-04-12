Imports System.IO

Public MustInherit Class myService : Inherits System.ServiceProcess.ServiceBase

#Region "Start argument variables"

    Private Enum eMode
        Switch
        Param
    End Enum

    Private _StartArg As New Dictionary(Of String, String)
    Public ReadOnly Property StartArg() As Dictionary(Of String, String)
        Get
            Return _StartArg
        End Get
    End Property

#End Region

#Region "Start"

    Public Overridable Sub svcStart(ByVal args As Dictionary(Of String, String))

    End Sub

    Protected Overrides Sub OnStart(ByVal args() As String)

        With _StartArg
            If args.Length > 0 Then

                Dim i As Integer = 0
                Dim m As eMode = eMode.Switch
                Dim thisSwitch As String = ""

                Do
                    Select Case args(i).Substring(0, 1)
                        Case "-", "/"
                            .Add(args(i).Substring(1), "")
                            thisSwitch = args(i).Substring(1)
                            m = eMode.Param
                        Case Else
                            Select Case m
                                Case eMode.Param
                                    .Item(thisSwitch) = args(i)
                                    thisSwitch = ""
                                    m = eMode.Switch

                                Case eMode.Switch
                                    .Add(args(i), "")

                            End Select

                    End Select
                    i += 1
                Loop Until i = args.Count

            End If

        End With

        If Not StartArg.Keys.Contains("debug") Then
            svcStart(StartArg)
        End If

    End Sub

#End Region

#Region "Pause"

    Public Overridable Sub svcPause()

    End Sub

    Protected Overrides Sub OnPause()
        svcPause()

    End Sub

#End Region

#Region "Continue"

    Public Overridable Sub svcContinue()

    End Sub

    Protected Overrides Sub OnContinue()
        If StartArg.Keys.Contains("debug") Then
            svcStart(StartArg)
        Else
            svcContinue()
        End If

    End Sub

#End Region

#Region "Stop"

    Public Overridable Sub svcStop()

    End Sub

    Protected Overrides Sub OnStop()
        svcStop()

    End Sub

#End Region

#Region "Logging"

    Private Function LogFolder() As DirectoryInfo
        Return New DirectoryInfo( _
            Path.Combine( _
                Environment.GetEnvironmentVariable("SystemRoot"), _
                String.Format( _
                    "logs\{0}\{1}", _
                    ServiceName, _
                    Now.ToString("yyyy-MM") _
                ) _
            ) _
        )

    End Function

    Private Function currentlog() As FileInfo
        With LogFolder()
            If Not .Exists Then .Create()
            Return New FileInfo( _
                Path.Combine( _
                    .FullName, _
                    String.Format( _
                        "{0}.txt", _
                        Now.ToString("yyMMdd") _
                    ) _
                ) _
            )

        End With

    End Function

    Public Sub Log(ByVal str, ByVal ParamArray args())
        Debug.Print("{0}> {1}", Format(Now, "hh:mm:ss"), String.Format(str, args))
        Using log As New StreamWriter(currentlog.FullName, True)
            log.WriteLine("{0}> {1}", Format(Now, "hh:mm:ss"), String.Format(str, args))
        End Using

    End Sub

#End Region

End Class
