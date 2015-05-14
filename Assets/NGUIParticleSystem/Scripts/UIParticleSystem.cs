/****************************************************************************
Copyright (c) 2014 dpull.com

http://www.dpull.com

ideas taken from:
    . The ocean spray in your face [Jeff Lander]
        http://www.double.co.nz/dust/col0798.pdf
    . Building an Advanced Particle System [John van der Burg]
        http://www.gamasutra.com/features/20000623/vanderburg_01.htm
    . LOVE game engine
        http://love2d.org/
****************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UIParticleSystem : UIWidget
{
    private List<UIParticle> Particles = new List<UIParticle>();
    private float EmitCounter;
    private float Elapsed;
    private bool IsPlaying = false;

    public UIAtlas Atlas;
    public string SpriteName;

    public UIParticleSimulationSpace SimulationSpace;

    public int MaxParticles;
    public float Angle;
    public float AngleVariance;
    public float Duration;
    public Color StartColor;
    public Color StartColorVariance;
    public Color FinishColor;
    public Color FinishColorVariance;
    public float StartParticleSize;
    public float StartParticleSizeVariance;
    public float FinishParticleSize;
    public float FinishParticleSizeVariance;
    public Vector2 SourcePosition;
    public Vector2 SourcePositionVariance;
    public float RotationStart;
    public float RotationStartVariance;
    public float RotationEnd;
    public float RotationEndVariance;
    public float ParticleLifespan;
    public float ParticleLifespanVariance;
    public float EmissionRate;
    public UIParticleMode EmitterType;

    // ParticleMode.Gravity
    public Vector2 Gravity;
    public float Speed;
    public float SpeedVariance;
    public float RadialAcceleration;
    public float RadialAccelVariance;
    public float TangentialAcceleration;
    public float TangentialAccelVariance;
    public bool RotationIsDir;

    // ParticleMode.Radius
    public float StartRadius;
    public float StartRadiusVariance;
    public float FinishRadius;
    public float FinishRadiusVariance;
    public float RotatePerSecond;
    public float RotatePerSecondVariance;

    protected override void OnStart()
    {
        base.OnStart();
        ResetSystem();
    }

    protected override void OnUpdate()
    {
        base.OnUpdate();
        
        if (!IsPlaying)
            return;

        if (EmissionRate > 0)
        {
            float rate = 1.0f / EmissionRate;
            //issue #1201, prevent bursts of particles, due to too high emitCounter
            if (Particles.Count < MaxParticles)
                EmitCounter += Time.deltaTime;
            
            while (EmitCounter > rate) 
            {
                EmitCounter -= rate;
                
                var particle = this.AddParticle();
                if (particle == null)
                    break;
            }

            Elapsed += Time.deltaTime;
            if (!Mathf.Approximately(this.Duration, -1f) && this.Duration < Elapsed)
                StopSystem();
        }
    }

    public void StopSystem()
    {
        IsPlaying = false;
    }

    public void ResetSystem()
    {
        foreach(var item in Particles)
        {
            GameObject.Destroy(item.gameObject);
        }
        Particles.Clear();

        EmitCounter = 0;
        Elapsed = 0;
        IsPlaying = true;
    }

    public UIParticle AddParticle()
    {
        if (Particles.Count == MaxParticles)
            return null;

        GameObject go;
        if (Particles.Count == 0)
        {
            var sprite = NGUITools.AddSprite(this.gameObject, this.Atlas, this.SpriteName);
            sprite.width = this.width;
            sprite.height = this.height;
            sprite.depth = this.depth;

            go = sprite.gameObject;
        }
        else
        {
            go = GameObject.Instantiate(Particles[0].gameObject) as GameObject;
            var oldParticle = go.GetComponent<UIParticle>();
            GameObject.DestroyImmediate(oldParticle);
        }

        go.transform.SetParent(this.transform, false);
        go.transform.localPosition = Vector3.zero;
        
        UIParticle particle;
        switch (EmitterType)
        {
            case UIParticleMode.Gravity:
                particle = go.AddComponent<UIGravityParticle>();
                break;
                
            case UIParticleMode.Radius:
                particle = go.AddComponent<UIRadiusParticle>();
                break;
                
            default:
                throw new UnityException("emitterType error");
        }  

        particle.ParticleSystem = this;
        Particles.Add(particle);
        return particle;
    }

    public void RemoveParticle(UIParticle particle)
    {
        GameObject.Destroy(particle.gameObject);
        Particles.Remove(particle);
    }
}

public enum UIParticleMode
{
    Gravity,
    Radius,
}

public enum UIParticleSimulationSpace
{
    Local,
    World
}

public class UIParticle : MonoBehaviour
{
    private UISprite Sprite;

    [System.NonSerialized] 
    public UIParticleSystem ParticleSystem;

    public Vector2 Pos;
    public Vector3 StartPos;        
    public Color Color;
    public Color DeltaColor;        
    public float Size;
    public float DeltaSize;        
    public float Rotation;
    public float DeltaRotation;        
    public float TimeToLive;

    void Start()
    {
        Sprite = this.GetComponent<UISprite>();
        Sprite.enabled = false;
        Setup();
    }

    void Update()
    {
        this.TimeToLive -= Time.deltaTime;
        if (this.TimeToLive <= 0)
        {
            ParticleSystem.RemoveParticle(this);
            return;
        }      

        Activate();

        switch (ParticleSystem.SimulationSpace)
        {
            case UIParticleSimulationSpace.World:
                this.transform.position = this.StartPos;
                this.transform.localPosition = this.transform.localPosition + new Vector3(this.Pos.x, this.Pos.y, 0);
                break;
                
            case UIParticleSimulationSpace.Local:
                this.transform.localPosition = this.StartPos + new Vector3(this.Pos.x, this.Pos.y, 0);
                break;                
        }

        this.transform.localScale = new Vector3(this.Size, this.Size, this.transform.localScale.z);
        this.transform.Rotate(this.transform.forward, this.Rotation);

        Sprite.color = this.Color;

        if (!Sprite.enabled)
            Sprite.enabled = true;
    }

    public static float Random(float value, float variance)
    {
        return value + variance * UnityEngine.Random.Range(-1f, 1f);
    }
    
    protected virtual void Setup()
    {
        var system = ParticleSystem;
        
        this.TimeToLive = Mathf.Max(0f, Random(system.ParticleLifespan, system.ParticleLifespanVariance));

        this.Pos.x = Random(system.SourcePosition.x, system.SourcePositionVariance.x);
        this.Pos.y = Random(system.SourcePosition.y, system.SourcePositionVariance.y);

        this.Color.r = Random(system.StartColor.r, system.StartColorVariance.r);
        this.Color.g = Random(system.StartColor.g, system.StartColorVariance.g);
        this.Color.b = Random(system.StartColor.b, system.StartColorVariance.b);
        this.Color.a = Random(system.StartColor.a, system.StartColorVariance.a);
        
        Color endColor;
        endColor.r = Random(system.FinishColor.r, system.FinishColorVariance.r);
        endColor.g = Random(system.FinishColor.g, system.FinishColorVariance.g);
        endColor.b = Random(system.FinishColor.b, system.FinishColorVariance.b);
        endColor.a = Random(system.FinishColor.a, system.FinishColorVariance.a);
        
        this.DeltaColor.r = (endColor.r - this.Color.r) / this.TimeToLive;
        this.DeltaColor.g = (endColor.g - this.Color.g) / this.TimeToLive;
        this.DeltaColor.b = (endColor.b - this.Color.b) / this.TimeToLive;
        this.DeltaColor.a = (endColor.a - this.Color.a) / this.TimeToLive;
        
        this.Size = Random(system.StartParticleSize, system.StartParticleSizeVariance);
        this.Size = Mathf.Max(0f, this.Size);
        
        if (Mathf.Approximately(system.FinishRadius, -1f))
        {
            this.DeltaSize = 0;
        }
        else
        {
            var end = Random(system.FinishParticleSize, system.FinishParticleSizeVariance);
            end = Mathf.Max(0f, end);
            
            this.DeltaSize = (end - this.Size) / this.TimeToLive;
        }
        
        this.Rotation = Random(system.RotationStart, system.RotationStartVariance);
        this.DeltaRotation = (Random(system.RotationEnd, system.RotationEndVariance) - this.Rotation) / this.TimeToLive;

        switch (system.SimulationSpace)
        {
            case UIParticleSimulationSpace.World:
                this.StartPos = this.transform.position;
                break;

            case UIParticleSimulationSpace.Local:
                this.StartPos = this.transform.localPosition;
                break;                
        }
    }

    protected virtual void Activate()
    {
        var dt = Time.deltaTime;

        // color
        this.Color.r += (this.DeltaColor.r * dt);
        this.Color.g += (this.DeltaColor.g * dt);
        this.Color.b += (this.DeltaColor.b * dt);
        this.Color.a += (this.DeltaColor.a * dt);
        
        // size
        this.Size += (this.DeltaSize * dt);
        this.Size = Mathf.Max(0, this.Size);
        
        // angle
        this.Rotation += (this.DeltaRotation * dt);
    }
}

public class UIGravityParticle : UIParticle
{
    public Vector2 Dir;
    public float RadialAccel;
    public float TangentialAccel;
    
    protected override void Setup()
    {
        base.Setup();

        var system = ParticleSystem;        
        var curAngle = (Random(system.Angle, system.AngleVariance)) * Mathf.PI / 180;
        var vector = new Vector2(Mathf.Cos(curAngle), Mathf.Sin(curAngle));
        var speed = Random(system.Speed, system.SpeedVariance);
        
        this.Dir = vector * speed;
        this.RadialAccel = Random(system.RadialAcceleration, system.RadialAccelVariance);
        this.TangentialAccel = Random(system.TangentialAcceleration, system.TangentialAccelVariance);

        if (system.RotationIsDir)
            this.Rotation = -(Mathf.Atan2(this.Dir.y, this.Dir.x) * 180 / Mathf.PI);
    }

    protected override void Activate()
    {        
        base.Activate();

        Vector2 tmp;
        Vector2 radial = Vector2.zero;
        Vector2 tangential;
        
        // radial acceleration
        if (this.Pos.x != 0 || this.Pos.y != 0)
            radial = this.Pos.normalized;

        tangential = radial;
        radial = radial * this.RadialAccel;
        
        // tangential acceleration
        float newy = tangential.x;
        tangential.x = -tangential.y;
        tangential.y = newy;
        tangential = tangential * this.TangentialAccel;
        
        // (gravity + radial + tangential) * dt
        tmp = radial + tangential + ParticleSystem.Gravity;
        tmp = tmp * Time.deltaTime;
        this.Dir = this.Dir + tmp;

        tmp = this.Dir * Time.deltaTime;
        this.Pos = this.Pos + tmp;
    }
}

public class UIRadiusParticle : UIParticle
{       
    public float Angle;
    public float DegreesPerSecond;
    public float Radius;
    public float DeltaRadius;
    
    protected override void Setup()
    {
        base.Setup();

        var system = ParticleSystem;
        
        this.Radius = Random(system.StartRadius, system.StartRadiusVariance);
        if (Mathf.Approximately(system.FinishRadius, -1f))
            this.DeltaRadius = 0;
        else
            this.DeltaRadius = (Random(system.FinishRadius, system.FinishRadiusVariance) - this.Radius) / this.TimeToLive;
        
        this.Angle = Random(system.Angle, system.AngleVariance) * Mathf.PI / 180;
        this.DegreesPerSecond = Random(system.RotatePerSecond, system.RotatePerSecondVariance) * Mathf.PI / 180;
    }

    protected override void Activate()
    {       
        base.Activate();
        
        // Update the angle and radius of the particle.
        this.Angle += this.DegreesPerSecond * Time.deltaTime;
        this.Radius += this.DeltaRadius * Time.deltaTime;
        this.Pos.x = - Mathf.Cos(this.Angle) * this.Radius;
        this.Pos.y = - Mathf.Sin(this.Angle) * this.Radius;
    }
}


