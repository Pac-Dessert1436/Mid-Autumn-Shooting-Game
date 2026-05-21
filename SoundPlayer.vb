Option Strict On
Option Infer On
Imports NAudio.Wave

Public NotInheritable Class SoundPlayer
    Implements IDisposable

    Private ReadOnly reader As AudioFileReader
    Private ReadOnly waveOut As WaveOutEvent
    Private isLooping As Boolean = False
    Private disposedValue As Boolean

    Public Sub New(filename As String)
        reader = New AudioFileReader(filename)
        waveOut = New WaveOutEvent
        waveOut.Init(reader)

        AddHandler waveOut.PlaybackStopped, AddressOf OnPlaybackStopped
    End Sub

    Public Sub PlayOnce()
        If waveOut IsNot Nothing Then
            isLooping = False
            If reader IsNot Nothing Then reader.Position = 0
            waveOut.Play()
        End If
    End Sub

    Public Sub PlayLooping()
        If waveOut IsNot Nothing Then
            isLooping = True
            If reader IsNot Nothing Then reader.Position = 0
            waveOut.Play()
        End If
    End Sub

    Public Sub [Stop]()
        If waveOut IsNot Nothing Then
            isLooping = False
            waveOut.Stop()
        End If
    End Sub

    Public Sub OnPlaybackStopped(sender As Object, e As StoppedEventArgs)
        If isLooping AndAlso waveOut IsNot Nothing Then
            If reader IsNot Nothing Then reader.Position = 0
            waveOut.Play()
        End If
    End Sub

    Private Sub Dispose(disposing As Boolean)
        If Not disposedValue Then
            If disposing Then
                reader.Dispose()
                waveOut.Dispose()
                RemoveHandler waveOut.PlaybackStopped, AddressOf OnPlaybackStopped
            End If
            disposedValue = True
        End If
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
        ' Do not change this code. Put cleanup code in 'Dispose(disposing As Boolean)' method
        Dispose(disposing:=True)
        GC.SuppressFinalize(Me)
    End Sub
End Class