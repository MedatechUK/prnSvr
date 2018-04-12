Imports System.IO
Imports System.Threading
Imports System.Drawing.Printing

Public Class prnSvr : Inherits myService

#Region "Public Properties"

    Public complete As New List(Of String)

    Public ReadOnly Property exepath() As DirectoryInfo
        Get
            Static ret As DirectoryInfo = Nothing
            If ret Is Nothing Then
                ret = New DirectoryInfo( _
                    Path.Combine( _
                        Environment.GetEnvironmentVariable("ProgramFiles(x86)"), _
                        "gs\gs9.23\bin" _
                    ) _
                )
            End If
            Return ret
        End Get

    End Property

    Private _syspath As DirectoryInfo = Nothing
    Public Property syspath() As DirectoryInfo
        Get
            If _syspath Is Nothing Then
                Try
                    _syspath = New DirectoryInfo( _
                         My.Computer.Registry.GetValue( _
                           "HKEY_LOCAL_MACHINE\SOFTWARE\prnsvr", _
                           "sysPath", _
                           Nothing _
                         ) _
                     )

                Catch ex As Exception
                    _syspath = Nothing

                End Try

            End If
            Return _syspath

        End Get

        Set(ByVal value As DirectoryInfo)
            My.Computer.Registry.SetValue( _
              "HKEY_LOCAL_MACHINE\SOFTWARE\prnsvr", _
              "sysPath", _
              value.FullName _
            )
            _syspath = value

        End Set

    End Property

#End Region

#Region "Override Service Methods"

    Overrides Sub svcStart(ByVal args As Dictionary(Of String, String))

        ' Verify installation of GhostScript
        If Not exepath.Exists Then
            Log("Ghostscript not found {0}.", exepath.FullName)
            End

        Else
            Log("exePath={0}", exepath.FullName)

        End If

        If args.Keys.Contains("dir") Then ' Set the monitor folder
            Try
                Log("Set Priority System path {0}.", args("dir"))
                Dim dir As New DirectoryInfo(args("dir"))
                With dir
                    If Not .Exists Then
                        Log("Create Priority System path {0}.", args("dir"))
                        .Create()
                    End If

                End With

                syspath = dir

            Catch ex As Exception
                Log("Invalid Priority System path {0}.", ex.Message)
                End

            End Try

        End If

        ' Verify monitor folder
        If Not syspath.Exists Then
            Log("Invalid Priority System path {0}.", syspath.FullName)
            End

        Else
            Log("sysPath={0}", syspath.FullName)

        End If

        ' For each installed printer
        For Each prin As String In PrinterSettings.InstalledPrinters
            With New DirectoryInfo(Path.Combine(syspath.FullName, prin))                
                If Not .Exists Then ' Create a folder
                    Log("Found printer \\{0}\{1}.", Environment.MachineName, prin)
                    .Create()

                End If
            End With
        Next

        ' Start folder monitoring
        StartHandler()

    End Sub

#End Region

#Region "FileSystemWatcher Handler"

    Private fsw As System.IO.FileSystemWatcher

    Private Sub StartHandler()

        fsw = New System.IO.FileSystemWatcher
        With fsw
            AddHandler .Created, AddressOf fsw_Created
            .Path = syspath.FullName
            .IncludeSubdirectories = True
            .EnableRaisingEvents = True

        End With

        Log("Started.")

    End Sub

    Private Sub fsw_Created(ByVal sender As Object, ByVal e As FileSystemEventArgs)

        ' Must be in a subdirectory
        If Not e.Name.Contains("\") Then Exit Sub

        Dim f As New fsEventArgs(Me, e)
        With f
            Select Case .FileExt
                Case ".pdf"
                    Log("Convert {0} to PCL printer code...", .Name)
                    With New Thread(AddressOf hConvert)
                        .Start(f)
                    End With

                Case ".pxl"
                    Log("Send {0} to the {1}", .Name, .PrinterName)
                    With New Thread(AddressOf hCopy)
                        .Start(f)
                    End With

            End Select
        End With

    End Sub

#End Region

#Region "Threads"

    Private Sub hCopy(ByVal e As fsEventArgs)
        With e
            Try
                .WaitWrite() ' for completion of PXL document
                File.Copy( _
                    .FullPath, _
                    .PrinterName _
                )

            Catch ex As Exception
                Log(ex.Message)

            Finally
                .DeleteAndWait()

            End Try

        End With

    End Sub

    Private Sub hConvert(ByVal e As fsEventArgs)
        With e
            Try
                Using proc As New Process
                    With proc
                        .StartInfo = e.ConvertProcessInfo
                        .Start() ' the conversion
                        While Not .HasExited ' the conversion
                            Thread.Sleep(100)

                        End While
                    End With

                    ' Inform the copy thread to proceed
                    complete.Add(.pxl.FullName)

                End Using

            Catch ex As Exception
                Log(ex.Message)

            Finally
                .DeleteAndWait()

            End Try

        End With

    End Sub

#End Region

End Class
