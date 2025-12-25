Option Strict On
Option Infer On
Imports System.MathF
Imports VbPixelGameEngine

Public NotInheritable Class Program
    Inherits PixelGameEngine

    ' Game assets
    Private ReadOnly sprPlayer As New Sprite("Assets/player.png") ' Rabbit spaceship
    Private ReadOnly sprMooncake As New Sprite("Assets/mooncake.png") ' Enemy
    Private ReadOnly sprBullet As New Sprite("Assets/bullet.png") ' Star bullet
    Private ReadOnly sprMoon As New Sprite("Assets/moon.png") ' Background moon
    Private ReadOnly bgmMainTheme As New SoundPlayer("Assets/main_theme.mp3")
    Private ReadOnly sndShoot As New SoundPlayer("Assets/shoot.wav")
    Private ReadOnly sndExplosion As New SoundPlayer("Assets/explosion.wav")
    Private ReadOnly sndLifeGained As New SoundPlayer("Assets/life_gained.wav")
    Private ReadOnly sndLifeLost As New SoundPlayer("Assets/life_lost.wav")

    ' Game objects and variables
    Private m_player As PlayerEntity
    Private ReadOnly m_bullets As New List(Of BulletEntity)
    Private ReadOnly m_mooncakes As New List(Of MooncakeEntity)
    Private ReadOnly m_particles As New List(Of Particle)
    Private m_moonOscillation As New Single

    Private m_score As Integer
    Private m_lives As Integer
    Private m_misses As Integer
    Private m_spawnTimer As Single
    Private m_spawnInterval As Single
    Private m_moonPos As New Vf2d(150, 60)
    Private m_bonusMult As Single

    Private Const BONUS_EVERY As Single = 1000

    Private Enum GameState
        Title = 0
        Playing = 1
        Paused = 2
        GameOver = 3
    End Enum

    Private Class TimedMessage
        Public timer As Single, active As Boolean, duration As Single, text As String

        Public Sub New(duration As Single, text As String)
            timer = 0
            active = False
            Me.duration = duration
            Me.text = text
        End Sub

        Public Sub ResetTimer()
            timer = 0
            active = False
        End Sub
    End Class

    Private Property CurrentState As GameState = GameState.Title

    Public Sub New()
        AppName = "Mid-Autumn Shooting Game"
    End Sub

    Friend Shared Sub Main()
        With New Program
            If .Construct(300, 400, fullScreen:=True) Then .Start()
        End With
    End Sub

    Protected Overrides Function OnUserCreate() As Boolean
        SetPixelMode(Pixel.Mode.Mask)

        ' Initialize player
        Dim playerStartPos As New Vf2d((ScreenWidth - sprPlayer.Width) / 2.0F, 320)
        m_player = New PlayerEntity(playerStartPos, sprPlayer)

        RestartGame()
        ' Start background music
        bgmMainTheme.PlayLooping()
        Return True
    End Function

    Protected Overrides Function OnUserUpdate(elapsedTime As Single) As Boolean
        ' Handle state transitions
        HandleStateInput()

        ' Clear screen with night sky color
        Clear(Presets.DarkBlue)

        Select Case CurrentState
            Case GameState.Title
                DrawTitleScreen()
            Case GameState.Playing
                UpdateGameLogic(elapsedTime)
                DrawGameElements()
            Case GameState.Paused
                DrawGameElements()
            Case GameState.GameOver
                DrawGameOverScreen()
        End Select

        Return Not GetKey(Key.ESCAPE).Pressed
    End Function

    Private Sub HandleStateInput()
        Select Case CurrentState
            Case GameState.Title
                If GetKey(Key.ENTER).Pressed Then CurrentState = GameState.Playing
            Case GameState.Playing
                If GetKey(Key.P).Pressed Then CurrentState = GameState.Paused
            Case GameState.Paused
                If GetKey(Key.P).Pressed Then CurrentState = GameState.Playing
            Case GameState.GameOver
                If GetKey(Key.R).Pressed Then
                    RestartGame()
                    CurrentState = GameState.Playing
                End If
        End Select
    End Sub

    Private Sub UpdateGameLogic(dt As Single)
        ' Update moon position with gentle oscillation
        m_moonOscillation += dt
        m_moonPos.x = 150 + Sin(m_moonOscillation * 0.5F) * 20

        ' Update player
        m_player.HandleInput(Me)
        m_player.Update(dt)
        m_player.KeepInBounds(ScreenWidth)

        ' Handle shooting
        Dim newBullet = m_player.TryFire(Me, dt, sprBullet)
        If newBullet IsNot Nothing Then
            m_bullets.Add(DirectCast(newBullet, BulletEntity))
            sndShoot.PlayOnce()
        End If

        ' Update bullets
        For i As Integer = m_bullets.Count - 1 To 0 Step -1
            m_bullets(i).Update(dt)

            ' Remove bullets that go off screen
            If m_bullets(i).IsOutOfBounds(ScreenWidth, ScreenHeight) Then
                m_bullets.RemoveAt(i)
            End If
        Next i

        ' Spawn mooncakes (enemies) with increasing difficulty
        m_spawnTimer += dt
        If m_spawnTimer >= m_spawnInterval Then
            Dim spawnX = RandomInt(20, ScreenWidth - sprMooncake.Width - 20)
            m_mooncakes.Add(
                New MooncakeEntity(New Vf2d(spawnX, -sprMooncake.Height), sprMooncake)
            )
            m_spawnTimer = 0

            ' Gradually increase difficulty by reducing spawn interval
            m_spawnInterval = Math.Max(0.3F, m_spawnInterval - 0.01F)
        End If

        Static ouchMessage As New TimedMessage(1.5F, "OUCH!")
        Static bonusMessage As New TimedMessage(2.5F, "Life +1, Misses -10")

        ' Update mooncakes
        For i As Integer = m_mooncakes.Count - 1 To 0 Step -1
            With m_mooncakes(i)
                .Update(dt)
                If .CollidesWith(m_player) Then
                    sndLifeLost.PlayOnce()
                    .Active = False
                    ouchMessage.active = True
                    m_lives -= 1
                    If m_lives <= 0 Then CurrentState = GameState.GameOver
                    Exit For
                End If

                ' Remove mooncakes that go off screen
                If .IsOutOfBounds(ScreenWidth, ScreenHeight) Then
                    m_mooncakes.RemoveAt(i)
                    m_misses += 1
                    If m_misses >= 100 Then CurrentState = GameState.GameOver
                End If
            End With
        Next i

        ' Update particles
        For i As Integer = m_particles.Count - 1 To 0 Step -1
            m_particles(i).Update(dt)
            If Not m_particles(i).IsAlive Then m_particles.RemoveAt(i)
        Next i

        If m_score >= m_bonusMult * BONUS_EVERY Then
            sndLifeGained.PlayOnce()
            m_lives += 1
            m_misses = Math.Max(0, m_misses - 10)
            bonusMessage.active = True
            m_bonusMult += 1
        End If

        DrawTimedMessage(dt, ouchMessage, m_player.pos - New Vi2d(0, 15))
        DrawTimedMessage(dt, bonusMessage, New Vi2d(140, 30))

        ' Check collisions
        For i As Integer = m_mooncakes.Count - 1 To 0 Step -1
            For j As Integer = m_bullets.Count - 1 To 0 Step -1
                If m_mooncakes(i).CollidesWith(m_bullets(j)) Then
                    ' Create explosion particles
                    CreateExplosionParticles(m_mooncakes(i).pos, 15)
                    ' Remove collided entities
                    m_mooncakes.RemoveAt(i)
                    m_bullets.RemoveAt(j)
                    ' Update score
                    m_score += If(Rnd > 0.5, 15, 10)
                    sndExplosion.PlayOnce()
                    Exit For
                End If
            Next j
        Next i
    End Sub

    Private Sub DrawTimedMessage(dt As Single, timedMsg As TimedMessage, pos As Vi2d)
        If timedMsg.active Then
            DrawString(pos, timedMsg.text, Presets.Snow)
            timedMsg.timer += dt
            If timedMsg.timer > timedMsg.duration OrElse
                CurrentState = GameState.GameOver Then timedMsg.ResetTimer()
        End If
    End Sub

    Private Sub CreateExplosionParticles(pos As Vf2d, count As Integer)
        For i As Integer = 1 To count
            m_particles.Add(New Particle(pos, New Pixel(
                RandomInt(200, 255),
                RandomInt(100, 200),
                RandomInt(0, 50)
            )))
        Next i
    End Sub

    Private Sub RestartGame()
        ' Reset all game state
        m_player.pos = New Vf2d((ScreenWidth - sprPlayer.Width) / 2.0F, 320)
        m_bullets.Clear()
        m_mooncakes.Clear()
        m_particles.Clear()
        m_score = 0
        m_lives = 3
        m_misses = 0
        m_spawnTimer = 0
        m_spawnInterval = 1.0F
        m_bonusMult = 1
    End Sub

    Private Sub DrawGameElements()
        ' Draw moon background
        DrawSprite(m_moonPos, sprMoon, 2)

        ' Draw player
        m_player.Draw(Me)

        ' Draw bullets, mooncakes and particle
        m_bullets.ForEach(Sub(bullet) bullet.Draw(Me))
        m_mooncakes.ForEach(Sub(mooncake) mooncake.Draw(Me))
        m_particles.ForEach(Sub(particle) particle.Draw(Me))

        ' Draw HUD
        DrawString(New Vi2d(10, 10), $"Score: {m_score,5}", Presets.Apricot)
        DrawString(New Vi2d(10, 30), $"Lives: {m_lives,2}", Presets.Apricot)
        DrawString(New Vi2d(180, 10), $"{m_misses,3}/100 Misses", Presets.Apricot)

        ' Draw instructions on game playing
        If CurrentState = GameState.Playing Then
            DrawString(New Vi2d(10, 360), "Move with LEFT or RIGHT", Presets.Snow)
            DrawString(New Vi2d(10, 380), "SPACE to shoot, P to pause", Presets.Snow)
        ElseIf CurrentState = GameState.Paused Then
            DrawString(New Vi2d(90, 300), "PAUSED", Presets.Yellow, 2)
            DrawString(New Vi2d(30, 360), "Press ""P"" to continue playing", Presets.Yellow)
        End If
    End Sub

    Private Sub DrawTitleScreen()
        DrawSprite(New Vi2d(m_moonPos), sprMoon, 2)

        ' Title text
        DrawString(New Vi2d(70, 100), "MID-AUTUMN", Presets.Orange, 2)
        DrawString(New Vi2d(50, 140), "SHOOTING GAME", Presets.Orange, 2)

        ' Instructions
        DrawString(New Vi2d(50, 270), "PRESS ""ENTER""", Presets.Beige, 2)
        DrawString(New Vi2d(30, 300), "TO START PLAYING", Presets.Beige, 2)
        DrawString(New Vi2d(30, 335), $"Special bonus at every {BONUS_EVERY} pts.", Presets.Beige, 1)

        ' Draw a small player preview
        DrawSprite(New Vi2d(135, 200), sprPlayer)
    End Sub

    Private Sub DrawGameOverScreen()
        DrawSprite(m_moonPos, sprMoon, 2)
        Dim message = If(m_misses >= 100, "You missed 100 mooncakes!", "You ran out of lives!")

        ' Game over text
        DrawString(New Vi2d(50, 280), message, Presets.White)
        DrawString(New Vi2d(70, 300), "GAME OVER", Presets.Yellow, 2)
        DrawString(New Vi2d(50, 330), $"Final Score: {m_score,5}", Presets.Yellow, 1)
        DrawString(New Vi2d(50, 350), "Press ""R"" to restart", Presets.Yellow, 1)
    End Sub
End Class