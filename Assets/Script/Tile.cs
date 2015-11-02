/// <summary>
/// Unity Match3D Tile Component class
/// Author: Andrei Victor (me@andreivictor.net)
/// https://www.github.com/elmachotroso/
/// </summary>

using UnityEngine;
using System.Collections;
using QsLib;

// Requirements
[RequireComponent( typeof( Rigidbody ) )]
[RequireComponent( typeof( MeshRenderer ) )]

/// This a representation of a tile in Unity 3D. This component will
/// manage the GameObject's Rigidbody Physics, and Material. This will
/// change the appearance based on the TileData from the TileGrid
/// class.
/// This class is expected to be a prefab and created and destroyed via
/// an Object Pool.
public class Tile : MonoBehaviour
{
	public static Color TileTypeToColor( TileGrid.TileTypes type )
	{
		Color color = Color.white;
		
		switch( type )
		{
		case TileGrid.TileTypes.Red:
			color = Color.red;
			break;
		case TileGrid.TileTypes.Yellow:
			color = Color.yellow;
			break;
		case TileGrid.TileTypes.Green:
			color = Color.green;
			break;
		case TileGrid.TileTypes.Blue:
			color = Color.blue;
			break;
		case TileGrid.TileTypes.Purple:
			color = Color.magenta;
			break;
		}		
		
		return color;
	}
	
	public TileGrid.TileData tileData
	{
	    get { return m_data; }
	    set
	    {
	    	m_data = value;
	    	UpdateTileVisualType();
	    }
	}
	
	public void EnablePhysics( bool enable )
	{
		if( m_rbody != null )
		{
			m_rbody.useGravity = enable;
			m_rbody.detectCollisions = enable;
			m_rbody.isKinematic = !enable;
			if( !enable )
			{
				m_rbody.velocity = Vector3.zero;
				m_rbody.angularVelocity = Vector3.zero;
			}
		}
	}
	
	public void SetVisible( bool enable )
	{
		if( m_renderer != null )
		{
			m_renderer.enabled = enable;
		}
	}
		
	public void Pop()
	{
		// TODO: sound		
		OnDestroy();
	}
	
	public bool IsAirborne()
	{
		if( m_rbody != null )
		{
			Vector3 below = transform.TransformDirection( Vector3.down ).normalized;
			float length = transform.localScale.y * 0.55f;
			bool isGroundBelow = Physics.Raycast( transform.position, below, length );
			//Debug.DrawLine( transform.position, transform.position + ( below * length ), Color.black, 2f, false );
			
			return !isGroundBelow || m_rbody.velocity.y != 0.0f;
		}
		
		return true;
	}
	
	protected void OnEnable()
	{
		Start();
	}
	
	protected void OnDisable()
	{
		OnDestroy();
	}
	
	protected void UpdateTileVisualType()
	{
		if( m_renderer != null && m_renderer.material != null )
		{
			m_renderer.material.color = ( m_data == null ) ? Color.white : TileTypeToColor( m_data.m_type );
		}
	}
	
	protected void Awake()
	{
	    m_rbody = GetComponent< Rigidbody >() as Rigidbody;
	    if( m_rbody == null )
	    {
	    	Debug.LogError( "Missing Rigidbody component." );
	    	enabled = false;
	    	return;
	    }
	    
		m_renderer = GetComponent< MeshRenderer >() as MeshRenderer;
		if( m_renderer == null || m_renderer.material == null )
	    {
			Debug.LogError ( "Missing MeshRenderer component and/or material." );
	    	enabled = false;
	    	return;
	    }
	}
	
	protected void Start()
	{
		EnablePhysics( true );
		UpdateTileVisualType();
		SetVisible( true );
	}
	
	protected void OnDestroy()
	{
		SetVisible( false );
		EnablePhysics( false );
	}
	
	protected TileGrid.TileData m_data      		= null;
	protected Rigidbody m_rbody             		= null;
	protected MeshRenderer m_renderer				= null;
}
