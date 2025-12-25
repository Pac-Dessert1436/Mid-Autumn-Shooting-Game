Option Strict On
Option Infer On
Imports VbPixelGameEngine
Imports VbPixelGameEngine.PixelGameEngine

Friend Module Randoms
    Private ReadOnly rnd As New Random

    Friend Function RandomInt(min As Integer, max As Integer) As Integer
        Return rnd.Next(min, max + 1)
    End Function

    Friend Function RandomFloat(min As Single, max As Single) As Single
        Return rnd.NextSingle() * (max - min) + min
    End Function
End Module

Friend Class Particle
    Private pos As Vf2d, size As Single, vel As Vf2d, color As Pixel, lifespan As Integer

    Private timer As Single : Const INTERVAL As Single = 0.05F

    Public Sub New(pos As Vf2d, color As Pixel)
        Me.pos = pos
        vel = New Vf2d With {
            .x = RandomFloat(-2.0F, 2.0F),
            .y = RandomFloat(-2.0F, 2.0F)
        }
        Me.color = color
        size = RandomFloat(2.0F, 5.0F)
        lifespan = RandomInt(20, 40)
    End Sub

    Public Sub Update(dt As Single)
        timer += dt
        If timer < INTERVAL Then Exit Sub
        pos += vel
        lifespan -= 1
        size = MathF.Max(0, size - 0.1F)
        timer = 0
    End Sub

    Public Sub Draw(pge As PixelGameEngine)
        If size > 0 Then pge.FillCircle(pos.x, pos.y, size, color)
    End Sub

    Public ReadOnly Property IsAlive As Boolean
        Get
            Return lifespan > 0 AndAlso size > 0
        End Get
    End Property
End Class

Friend MustInherit Class Entity
    Public pos As Vf2d
    Public ReadOnly sprite As Sprite
    Public ReadOnly size As Vf2d
    Public vel As New Vf2d
    Public Property Active As Boolean = True

    Public Sub New(pos As Vf2d, sprite As Sprite)
        Me.pos = pos
        Me.sprite = sprite
        size = New Vf2d(sprite.Width, sprite.Height)
    End Sub

    Public Sub Draw(pge As PixelGameEngine, Optional scale As Integer = 1)
        If Active Then pge.DrawSprite(pos.x, pos.y, sprite, scale)
    End Sub

    Public Overridable Sub Update(elapsedTime As Single)
        If Active Then pos += vel * elapsedTime
    End Sub

    Public Function CollidesWith(entity As Entity) As Boolean
        If Not Active OrElse Not entity.Active Then Return False

        ' Simple axis-aligned bounding box collision
        Return pos.x < entity.pos.x + entity.size.x AndAlso
                pos.x + size.x > entity.pos.x AndAlso
                pos.y < entity.pos.y + entity.size.y AndAlso
                pos.y + size.y > entity.pos.y
    End Function

    ' Check if entity is outside screen bounds
    Public Function IsOutOfBounds(screenWidth As Integer, screenHeight As Integer) As Boolean
        Return pos.x + size.x < 0 OrElse pos.x > screenWidth OrElse
               pos.y + size.y < 0 OrElse pos.y > screenHeight
    End Function
End Class

' Player-specific entity with movement controls
Friend Class PlayerEntity
    Inherits Entity

    Private Const MOVE_SPEED As Single = 200.0F
    Private Const FIRE_RATE As Single = 0.3F

    Private fireCooldown As Single = 0.0F

    Public Sub New(pos As Vf2d, sprite As Sprite)
        MyBase.New(pos, sprite)
    End Sub

    Public Sub HandleInput(pge As PixelGameEngine)
        ' Reset velocity
        vel = New Vf2d(0, 0)

        ' Handle movement
        If pge.GetKey(Key.LEFT).Held Then
            vel.x = -MOVE_SPEED
        ElseIf pge.GetKey(Key.RIGHT).Held Then
            vel.x = MOVE_SPEED
        End If
    End Sub

    Public Function TryFire(pge As PixelGameEngine, dt As Single, bulletSprite As Sprite) As Entity
        fireCooldown -= dt

        If pge.GetKey(Key.SPACE).Held AndAlso fireCooldown <= 0 Then
            fireCooldown = FIRE_RATE
            ' Spawn bullet slightly above player
            Dim bulletPos As New Vf2d(
                pos.x + (size.x - bulletSprite.Width) / 2,
                pos.y - bulletSprite.Height
            )
            Return New BulletEntity(bulletPos, bulletSprite)
        End If
        If pge.GetKey(Key.SPACE).Released Then fireCooldown = 0

        Return Nothing
    End Function

    Public Sub KeepInBounds(screenWidth As Integer)
        ' Keep player within horizontal bounds
        pos.x = Math.Max(0, Math.Min(screenWidth - size.x, pos.x))
    End Sub
End Class

' Bullet entity with upward movement
Friend Class BulletEntity
    Inherits Entity

    Private Const BULLET_SPEED As Single = 400.0F

    Public Sub New(pos As Vf2d, sprite As Sprite)
        MyBase.New(pos, sprite)
        vel.y = -BULLET_SPEED ' Move upward
    End Sub
End Class

' Mooncake enemy entity
Friend Class MooncakeEntity
    Inherits Entity

    Private Const BASE_SPEED As Single = 150.0F

    Public Sub New(pos As Vf2d, sprite As Sprite)
        MyBase.New(pos, sprite)
        vel.y = BASE_SPEED + RandomFloat(-30.0F, 30.0F) ' Slight variation in speed
    End Sub
End Class