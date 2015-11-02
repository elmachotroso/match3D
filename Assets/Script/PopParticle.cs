/// <summary>
/// Unity Match3D Pop Particle Component class
/// Author: Andrei Victor (me@andreivictor.net)
/// https://www.github.com/elmachotroso/
/// </summary>

using UnityEngine;
using System.Collections;
using QsLib;

[RequireComponent( typeof( ParticleSystem ) ) ]

// The Pop Particle component performs the playback and auto unspawns at the end of the playback.
public class PopParticle : MonoBehaviour
{
    public delegate void UnspawnFunction( GameObject go );
    
    // Sets the unspawn function for the pop particle. It is expected that this function interacts with the pop
    // particle's object pool. This will be the key in order for a proper destruction after particle finishes.
    public void SetUnspawnFunction( UnspawnFunction func )
    {
        m_unspawn = func;
    }
    
    protected void Awake()
    {
        m_particle = GetComponent< ParticleSystem >() as ParticleSystem;
        if( m_particle == null )
        {
            DebugUtil.LogError( "No particle system found on Pop Particle" );
            enabled = false;
            return;
        }
    }
    
    protected void Start()
    {
		m_done = false;
		if( m_particle != null )
		{
			m_particle.Play();
		}
    }
    
    protected void OnDestroy()
    {
        if( m_particle != null )
        {
			m_particle.Stop();
			m_particle.Clear();
        }
    }
    
    protected void Update()
    {
        if( !m_done && m_particle != null )
        {
            if( !m_particle.IsAlive() )
            {
				m_done = true;
                if( m_unspawn != null )
                {
					DebugUtil.LogWarning( "2" );
                    m_unspawn( gameObject );
                }
            }
        }
    }
    
    protected void OnEnable()
    {
        Start();
    }
    
    protected void OnDisable()
    {
        OnDestroy();
    }
    
    private bool m_done                 = false;
    private UnspawnFunction m_unspawn   = null;
    private ParticleSystem m_particle   = null;
}
