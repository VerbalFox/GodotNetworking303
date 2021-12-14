using Godot;
using System;

public class Player : KinematicBody
{
    NetworkManager networkManager;
    public int playerId = 0;
    public Vector3 mostRecentReceivedPosition = new Vector3();
    public Vector3 mostRecentReceivedVelocity = new Vector3();
    public double timeSinceLastPositionUpdate = 0;
    public double timeLastPositionReceived = 0;

    //Mouse variables
    [Export] float mouseSensitivity = 0.3f;

    //Fly variables
    [Export] float flySpeed = 12f;
    [Export] float flyAcceleration = 7f;
    bool flyMode = false;
    Vector3 flyVelocity;

    //Move variables
    [Export] float gravity = -9.8f;
    [Export] float gravityMultiplier = 2f;
    [Export] float moveSpeed = 12f;
    [Export] float moveWalkSpeed = 6f;
    [Export] float moveAcceleration = 7f;
    [Export] float moveDeceleration = 9f;
    [Export] public bool canMove = true;
    bool isWalking = false;
    bool forcedWalk = false;
    
    public Vector3 velocity;
    
    //Jump variables
    [Export] float jumpSpeed = 30f;
    int airJumps = 1;
    int airJumpsLeft;
    bool canJump = false;
    bool alowJumpInput = true;
    bool hasFloorContact = false;
    

    //View variables
    [Export] float maxAngleView = 90f;
    [Export] float minAngleView = -90f;
    float cameraAngle = 0f;
    float headRelativeAngle = 0f;

    //Slope variables
    [Export] float maxSlopeAngle = 35f;

    Camera camera;
    Spatial head;
    RayCast floorChecker;
    GeometryInstance mesh;
    Vector3 direction;
    bool canDash = true;
    bool isDashing = false;
    float timeDashing = 0f;

    public override void _Ready()
    {
        GetNodes();
        gravity = gravity * gravityMultiplier;
        airJumpsLeft = airJumps;

        Input.SetMouseMode(Input.MouseMode.Captured);

        networkManager = GetNode<NetworkManager>("/root/NetworkManager");
    }

    public override void _PhysicsProcess(float delta)
    {
        RotateView();

        if (flyMode)
        {
            MoveCharacterFly(delta);
        }
        else
        {
            if (canMove) {
                MoveCharacter(delta);
                CalculateMoveToFloor(delta);

            } else {
                MoveCharacterNetworked(delta);
            }
        }

        try {
            if (isDashing && GetLastSlideCollision().Collider is Player collidedPlayer)
            {
                KinematicCollision kinematicCollision = GetLastSlideCollision();
                isDashing = false;
                timeDashing = 0f;
                
                if (networkManager.isServer) {
                    networkManager.SendCollisionClient(kinematicCollision.Position, kinematicCollision.Normal, collidedPlayer.GlobalTransform.origin, collidedPlayer.playerId);
                } else {
                    networkManager.SendCollisionServer(kinematicCollision.Position, kinematicCollision.Normal, collidedPlayer.GlobalTransform.origin, collidedPlayer.playerId);
                }
                //collidedPlayer.GlobalTransform.origin;
            }
        } catch (NullReferenceException e) {
            //GD.Print(e.Message);
        }
    }

    private void MoveCharacterNetworked(float delta)
    {
        timeSinceLastPositionUpdate += delta;
        
        GlobalTransform = new Transform(
            GlobalTransform.basis.Column0,
            GlobalTransform.basis.Column1,
            GlobalTransform.basis.Column2,
            GlobalTransform.origin.LinearInterpolate(mostRecentReceivedPosition + (mostRecentReceivedVelocity * (float)timeSinceLastPositionUpdate), 0.5f)
        );
        
    }

    public override void _Process(float delta)
    {
        ResetJumpCount();
        UpdateMovementInput();

        if (isDashing) {
            timeDashing += delta;
        }

        if (timeDashing > .5f) {
            timeDashing = 0f;
            isDashing = false;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!isDashing) UpdateCameraInput(@event);
    }

    void UpdateMovementInput()
    {
        direction = Vector3.Zero;

        Basis headDirection = head.GetGlobalTransform().basis;
        Basis cameraDirection = camera.GetGlobalTransform().basis;

        if (flyMode)
        {
            if (canMove && !isDashing) CalculateFlyDirection(cameraDirection);
        }
        else
        {
            if (canMove && !isDashing) CalculateMoveDirection(cameraDirection, headDirection);
            if (canMove && isDashing) CalculateDashDirection(cameraDirection);
        }

        if (Input.IsActionJustPressed("dash") && canDash) {
            isDashing = true;
            canDash = false;
        }

        direction = direction.Normalized();

        isWalking = Input.IsActionPressed("move_walk");

        if (Input.IsActionJustPressed("jump") && alowJumpInput)
        {
            canJump = true;
        }
        else if (Input.IsActionJustPressed("jump") && airJumpsLeft > 0)
        {
            canJump = true;
            airJumpsLeft--;
        }
        else
        {
            canJump = false;
        }
        
        if (Input.IsActionJustPressed("fly_mode")) flyMode = !flyMode;   
    }

    void MoveCharacter(float delta)
    {
        Vector3 tempVelocity = velocity;
        tempVelocity.y = 0f;

        float speed;
        speed = isWalking || forcedWalk ? moveWalkSpeed : moveSpeed;
        speed = isDashing ? 50f : speed;

        Vector3 target = direction * speed;

        if (!isDashing) {
            float acceleration;
            if (direction.Dot(tempVelocity) > 0)
            {
                acceleration = moveAcceleration;
            }
            else
            {
                acceleration = moveDeceleration;
            }

            tempVelocity = tempVelocity.LinearInterpolate(target, acceleration * delta);
            velocity.x = tempVelocity.x;
            velocity.z = tempVelocity.z;

            if (canJump)
            {
                velocity.y += jumpSpeed;
                hasFloorContact = false;
            }
        }
        else {
            tempVelocity = target;
            velocity.x = tempVelocity.x;
            velocity.y = tempVelocity.y;
            velocity.z = tempVelocity.z;
        }

        velocity = MoveAndSlide(velocity, Vector3.Up);
    }

    void MoveCharacterFly(float delta)
    {
        Vector3 target = direction * flySpeed;

        flyVelocity = flyVelocity.LinearInterpolate(target, flyAcceleration * delta);
        MoveAndSlide(flyVelocity);
    }

    void UpdateCameraInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion mouseMotion)
        {
            headRelativeAngle = Mathf.Deg2Rad(-mouseMotion.Relative.x * mouseSensitivity);

            cameraAngle = Mathf.Clamp(cameraAngle - mouseMotion.Relative.y * mouseSensitivity, minAngleView, maxAngleView);
        }
    }

    void GetNodes()
    {
        head = (Spatial) GetNode("Head");
        camera = (Camera) GetNode("Head/Camera");
        floorChecker = (RayCast) GetNode("FloorChecker");
        mesh = (GeometryInstance) GetNode("Mesh");
    }

    void RotateView()
    {
        head.RotateY(headRelativeAngle);
        camera.SetRotationDegrees(new Vector3(cameraAngle, 0f, 0f));
        headRelativeAngle = 0f;
    }
    
    void CalculateDashDirection(Basis cameraDirection){
        direction -= cameraDirection.z;
    }

    void CalculateFlyDirection(Basis cameraDirection)
    {
        if (Input.IsActionPressed("move_forward")) direction -= cameraDirection.z;
        if (Input.IsActionPressed("move_backward")) direction += cameraDirection.z;
        if (Input.IsActionPressed("move_left")) direction -= cameraDirection.x;
        if (Input.IsActionPressed("move_right")) direction += cameraDirection.x;
    }

    void CalculateMoveDirection(Basis cameraDirection, Basis headDirection)
    {
        if (Input.IsActionPressed("move_forward")) direction -= headDirection.z;
        if (Input.IsActionPressed("move_backward")) direction += headDirection.z;
        if (Input.IsActionPressed("move_left")) direction -= cameraDirection.x;
        if (Input.IsActionPressed("move_right")) direction += cameraDirection.x;
    }

    void CalculateMoveToFloor(float delta)
    {   
        if (IsOnFloor())
        {
            hasFloorContact = true;
            forcedWalk = false;
            alowJumpInput = true;

            Vector3 floorNormal = floorChecker.GetCollisionNormal();
            float floorAngle = Mathf.Rad2Deg(Mathf.Acos(floorNormal.Dot(Vector3.Up)));

            if (floorAngle > maxSlopeAngle)
            {
                forcedWalk = true;
                airJumpsLeft = 0;
                Fall(delta);
            }
        }
        else
        {
            if (!floorChecker.IsColliding())
            {
                hasFloorContact = false;
                Fall(delta);
            }
        }
        if (hasFloorContact && !IsOnFloor()) {
            MoveAndCollide(Vector3.Down);
            canDash = true;    
        };
    }

    void Fall(float delta)
    {
        velocity.y += gravity * delta;
        alowJumpInput = false;
    }

    public void CameraVisible(bool isVisible) {
        camera.Current = isVisible;
    }

    public void Launch(Vector3 position, Vector3 normal, Vector3 playerPosition) {
        int launchPower = 1000;
        velocity.y += 25;
        hasFloorContact = false;
        velocity.x += normal.x * launchPower;
        velocity.z += normal.z * launchPower;

        GD.Print("Launch called");
        GlobalTransform = new Transform(
            GlobalTransform.basis.Column0,
            GlobalTransform.basis.Column1,
            GlobalTransform.basis.Column2,
            playerPosition
        );
    }

    void ResetJumpCount()
    {
        if (alowJumpInput && airJumpsLeft == 0)
        {
            airJumpsLeft = airJumps;
        }
    }
}