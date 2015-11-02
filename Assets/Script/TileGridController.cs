using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using QsLib;

public class TileGridController : MonoBehaviour
{
	protected void Awake()
	{
		if( m_TilePrefab == null || m_PlatformPrefab == null || m_PopPrefab == null )
		{
			DebugUtil.LogError( "Some of the prefabs are not set up yet." );
			enabled = false;
			return;
		}
		
		m_grid = new TileGrid( m_width, m_height );
		if( m_grid == null )
		{
			DebugUtil.LogError( "TileGrid initialization error!" );
			enabled = false;
			return;
		}
		
		ComputeDimensions();
		
		m_tiles = new ObjectPool< GameObject >( m_width * m_height, TileConstructor, TileDestructor );
		/*m_pops = new ObjectPool< GameObject >( m_PopPrefab, m_width * m_height );
		if( m_tiles == null || m_pops == null )
		{
			DebugUtil.LogError( "There was a problem when creating the object pools." );
			enabled = false;
			return;
		}*/
		
		SetupGrid();
		
		m_fsm = new Fsm< TileGridController >( this );
		m_fsm.AddState( new RefillGrid( m_fsm, this ) );
		m_fsm.AddState( new EvaluteGrid( m_fsm, this ) );
		m_fsm.AddState( new Waiting( m_fsm, this ) );
		m_fsm.AddState( new SwappingTiles( m_fsm, this ) );
		m_fsm.TransitionTo< RefillGrid >(); // Start in this state.
	}
	
	protected void Update()
	{
		m_fsm.Update( Timers.Instance.Game.GetDeltaTime() );
	}
	
	private void ComputeDimensions()
	{
		m_BlockSize = m_TilePrefab.transform.localScale;
		m_PlatformThickness = m_PlatformPrefab.transform.localScale.y;
		float halfHDistance = m_width * 0.5f * m_BlockSize.x;
		float halfVDistance = m_height * 0.5f * m_BlockSize.y;
		m_topLeft = new Vector2( -halfHDistance, halfVDistance );
		m_bottomRight = new Vector2( halfHDistance, -halfVDistance );
	}
	
	private bool SetupGrid()
	{
		m_activeScoring = false;
		
		// Create the platform
		GameObject platform = GameObject.Instantiate< GameObject >( m_PlatformPrefab );
		if( platform == null )
		{
			DebugUtil.LogError( "Failed to create the grid platform" );
			return false;
		}
		
		Vector3 offset = new Vector3(
			0.0f,
			m_bottomRight.y - ( m_BlockSize.y * 0.5f ) - ( m_PlatformThickness * 0.5f ),
			0.0f
			);		
		platform.transform.position = gameObject.transform.position + offset;
		platform.transform.rotation = gameObject.transform.rotation;
		platform.transform.localScale = new Vector3( m_width * m_BlockSize.x, m_PlatformThickness, m_BlockSize.z );
		platform.transform.SetParent( gameObject.transform );
		
		// Create the spawn points for tile drop off.
		m_spawnPoints = new GameObject[ m_width ];
		if( m_spawnPoints == null || m_spawnPoints.Length != m_width )
		{
			DebugUtil.LogError( "Problem in creating spawn points array." );
			return false;
		}
		
		for( int i = 0; i < m_spawnPoints.Length; ++i )
		{
			GameObject go = new GameObject();
			if( go != null )
			{
				go.name = string.Format( "TileSpawner {0}", i );
				offset = new Vector3( m_topLeft.x + ( m_BlockSize.x * 0.5f ) + m_BlockSize.x * i,
					m_topLeft.y + m_BlockSize.y,
					0.0f );
				go.transform.position = gameObject.transform.position + offset;
				go.transform.rotation = gameObject.transform.rotation;
				go.transform.localScale = m_BlockSize;
				go.transform.SetParent( gameObject.transform );
				m_spawnPoints[ i ] = go;
			}
		}
		
		// Create column counter for the spawner.
		m_columnCounts = new int[ m_width ];
		for( int i = 0; i < m_columnCounts.Length; ++i )
		{
			m_columnCounts[ i ] = 0;
		}
		
		// Create quick collection of tiles out.
		m_tilesOut = new List< Tile >();
		
		return true;
	}
	
	protected GameObject SpawnTile( int column, TileGrid.TileData tileData )
	{
		GameObject tileGo = m_tiles.GetReadyObject();
		if( tileGo != null )
		{
			tileGo.transform.position = m_spawnPoints[ column ].transform.position;
			tileGo.SetActive( true );
			Tile tileScript = tileGo.GetComponent< Tile >() as Tile;
			if( tileScript != null )
			{
				m_tilesOut.Add( tileScript );
				tileScript.tileData = tileData;
			}
			m_columnCounts[ column ]++;
		}
		
		return tileGo;
	}
	
	protected void UnspawnTile( GameObject tileGo )
	{
		if( tileGo == null )
		{
			return;
		}
		
		Tile tile = tileGo.GetComponent< Tile >() as Tile;
		if( tile != null )
		{
			Vector2 coords = m_grid.GetTileCoords( tile.tileData );
			if( coords.x == TileGrid.INVALID_INDEX )
			{
				DebugUtil.LogError( "This should never happen!" );
			}
			
			tileGo.transform.position = m_spawnPoints[ (int) coords.x ].transform.position;
			m_columnCounts[ (int) coords.x ]--;
			tileGo.SetActive( false );
			m_tiles.ReturnObject( tile.gameObject );
		}
	}
	
	private GameObject TileConstructor()
	{
		GameObject go = GameObject.Instantiate( m_TilePrefab ) as GameObject;
		if( go != null )
		{
			go.transform.SetParent( gameObject.transform );
		}
		return go;
	}
	
	private void TileDestructor( GameObject go )
	{
		if( go != null )
		{
			GameObject.Destroy( go );
		}		
	}
	
	private void OnGUI()
	{
		if( !m_debugMode )
		{
			return;
		}
		
		if( m_grid == null )
		{
			return;
		}
		
		Color defBgColor = GUI.backgroundColor;
		for( int x = 0; x < m_width; ++x )
		{
			for( int y = 0; y < m_height; ++y )
			{
				int tileIndex = m_grid.GetTileIndex( x, y );				
				TileGrid.TileTypes type = m_grid.GetTile( tileIndex ).m_type;
				GUI.backgroundColor = Tile.TileTypeToColor( type );
				GUI.contentColor = Color.white;
								
				if( GUI.Button( new Rect( m_debugGridPos.x + ( x * m_debugTileSize ),
				                         m_debugGridPos.y + ( y * m_debugTileSize ),
				                         m_debugTileSize, m_debugTileSize ),
				               type.ToString() ) )
				{
					// Do nothing when button pressed.
				}
			}
		}
		GUI.backgroundColor = defBgColor;
	}
	
	[SerializeField] protected GameObject m_TilePrefab		= null;
	[SerializeField] protected GameObject m_PlatformPrefab	= null;
	[SerializeField] protected GameObject m_PopPrefab		= null;
	[SerializeField] protected int m_width      			= 7;
	[SerializeField] protected int m_height     			= 7;
	[SerializeField] protected float m_tileRespawnDelay		= 2.0f;
	[SerializeField] protected bool m_debugMode				= false;
	[SerializeField] protected float m_debugTileSize		= 10;
	[SerializeField] protected Vector2 m_debugGridPos		= Vector2.zero;
	
	protected Fsm< TileGridController > m_fsm				= null;
	protected Tile m_mouseDownTile                          = null;
	protected Tile m_mouseUpTile                            = null;
	private float m_PlatformThickness						= 0.24f;
	private Vector3 m_BlockSize								= Vector3.zero;
	private Vector2 m_topLeft								= Vector2.zero;
	private Vector2 m_bottomRight							= Vector2.zero;
	private GameObject[] m_spawnPoints						= null;
	private int[] m_columnCounts							= null;
	private TileGrid m_grid									= null;
	private ObjectPool< GameObject > m_tiles				= null;
	private ObjectPool< GameObject > m_pops					= null;
	private List< Tile > m_tilesOut							= null;
	private bool m_activeScoring							= false;
	private int m_lastMatchesResult							= 0;
	
	public class RefillGrid : FsmState< TileGridController >
	{
		public RefillGrid( Fsm< TileGridController > fsm, TileGridController parent ) : base( fsm, parent )
		{
		}
		
		public override void OnEnter()
		{
			base.OnEnter();
			
			// We enable all gravity for the tiles out.
			// Note that newly created tiles are always gravity enabled.
			// Refer to Tile.cs if you wish.
			
			for( int i = 0; i < parent.m_tilesOut.Count; ++i )
			{
				Tile tile = parent.m_tilesOut[ i ];
				if( tile != null )
				{
					tile.EnablePhysics( true );
				}
			}
		}
		
		public override void OnUpdate( float dt )
		{
			base.OnUpdate( dt );
			
			int size = parent.m_columnCounts.Length;
			if( Timers.Instance.Game.GetTime() >= m_nextTimeToSpawn )
			{
				for( int i = 0; i < size; ++i )
				{
					if( parent.m_columnCounts[ i ] < parent.m_height )
					{
						int x = i;
						int y = parent.m_height - 1 - parent.m_columnCounts[ i ];
						parent.SpawnTile( i, parent.m_grid.GetTile( x, y ) );
					}
				}
				
				m_nextTimeToSpawn = Timers.Instance.Game.GetTime() + parent.m_tileRespawnDelay;
			}
			
			bool isTilesComplete = true;
			for( int i = 0; i < size; ++i )
			{
				if( parent.m_columnCounts[ i ] != parent.m_height )
				{
					isTilesComplete = false;
					break;
				}
			}
			
			bool isAllAtRest = true;
			for( int i = 0; i < parent.m_tilesOut.Count; ++i )
			{
				Tile tile = parent.m_tilesOut[ i ];
				if( tile == null || tile.IsAirborne() )
				{
					isAllAtRest = false;
					break;
				}
			}
			
			if( isTilesComplete && isAllAtRest )
			{
				if( parent.m_lastMatchesResult > 0 )
				{
					DebugUtil.Log("Going to eval.");
					fsm.TransitionTo< EvaluteGrid >();
				}
				else
				{
					DebugUtil.Log("Going to waiting.");
					fsm.TransitionTo< Waiting >();
				}
			}
			else
			{
				if( parent.m_debugMode )
				{
					DebugUtil.LogWarning( string.Format( "isTilesComplete: {0}\nisAllAtRest: {1}",
						isTilesComplete, isAllAtRest ) );
				}
			}
		}
		
		public override void OnExit()
		{
			base.OnExit();
			
			// We disable all gravity for the tiles out.
			
			for( int i = 0; i < parent.m_tilesOut.Count; ++i )
			{
				Tile tile = parent.m_tilesOut[ i ];
				if( tile != null )
				{
					tile.EnablePhysics( false );
				}
			}
		}
		
		private float m_nextTimeToSpawn		= 0.0f;
	}
	
	public class EvaluteGrid : FsmState< TileGridController >
	{
		public EvaluteGrid( Fsm< TileGridController > fsm, TileGridController parent ) : base( fsm, parent )
		{
		}
		
		public override void OnEnter()
		{
			base.OnEnter();
			
			//m_chains = -1; // first matches don't count as a chain.
			
			
			parent.m_grid.DoMatchChecking();
			
			// Pop the to-be-claimed matches
			for( int i = parent.m_tilesOut.Count - 1; i >= 0; --i )
			{
				Tile tile = parent.m_tilesOut[ i ];
				if( tile != null && tile.tileData.m_isMatched )
				{
					tile.Pop();
					if( parent.m_activeScoring )
					{
						// TODO: Give score
					}
					
					parent.UnspawnTile( tile.gameObject );					
					parent.m_tilesOut.RemoveAt( i );
				}
			}
			
			int matches = parent.m_grid.DoClaimMatches();
			parent.m_lastMatchesResult = matches;
			
			if( parent.m_activeScoring )
			{
				// TODO: event for matching
				m_matches += matches;
				if( matches > 0 )
				{
					++m_chains;
					if( m_chains > 0 )
					{
						// TODO: event for chaining (pass m_chains value)
					}
				}
			}
			parent.m_grid.DoUnmarkAllTiles();
			parent.m_grid.DoTileMovements();
			fsm.TransitionTo< RefillGrid >();	
		}
		
		public override void OnUpdate( float dt )
		{
			base.OnUpdate( dt );
		}
		
		public override void OnExit()
		{
			base.OnExit();
		}
		
		private int m_matches		= 0;
		private int m_chains		= -1;
	}
	
	public class Waiting : FsmState< TileGridController >
	{
		public Waiting( Fsm< TileGridController > fsm, TileGridController parent ) : base( fsm, parent )
		{
		}
		
		public override void OnEnter()
		{
			base.OnEnter();
			
			parent.m_mouseDownTile = null;
			parent.m_mouseUpTile = null;
		}
		
		public override void OnUpdate( float dt )
		{
			if( !Input.mousePresent )
			{
				return;
			}
			
			if( Input.GetMouseButtonDown( 0 ) )
			{
				Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				RaycastHit hit;
				if( Physics.Raycast( ray, out hit ) )
				{
					GameObject go = hit.collider.gameObject;
					if( go != null )
					{
						parent.m_mouseDownTile = go.GetComponent< Tile >() as Tile;
					}
				}
			}
			
			if( parent.m_mouseDownTile != null && Input.GetMouseButtonUp( 0 ) )
			{
				Ray ray = Camera.main.ScreenPointToRay( Input.mousePosition );
				RaycastHit hit;
				if( Physics.Raycast( ray, out hit ) )
				{
					GameObject go = hit.collider.gameObject;
					if( go != null )
					{
						parent.m_mouseUpTile = go.GetComponent< Tile >() as Tile;
						if( parent.m_mouseUpTile != null )
						{
							TileGrid grid = parent.m_grid;
							int tileIndexDown = grid.GetTileIndex( parent.m_mouseDownTile.tileData );
							int tileIndexUp = grid.GetTileIndex( parent.m_mouseUpTile.tileData );
							if( tileIndexDown != TileGrid.INVALID_INDEX
								&& tileIndexUp != TileGrid.INVALID_INDEX )
							{
								if( grid.IsTileAdjacent( tileIndexDown, tileIndexUp ) )
								{
									grid.SwapTile( tileIndexDown, tileIndexUp );
									if( grid.IsMatchingAnyAdjacentTiles( tileIndexDown )
										|| grid.IsMatchingAnyAdjacentTiles( tileIndexUp ) )
									{
										// apply scores
										DebugUtil.LogWarning( "Has matches" );
										fsm.TransitionTo< SwappingTiles >();
									}
									else
									{
										DebugUtil.LogWarning( "No matches." );
										grid.SwapTile( tileIndexDown, tileIndexUp );
									}
								}
							}
						}
					}
				}
			}
		}
	}
	
	public class SwappingTiles : FsmState< TileGridController >
	{
		public SwappingTiles( Fsm< TileGridController > fsm, TileGridController parent ) : base( fsm, parent )
		{
		}
		
		public override void OnEnter()
		{
			base.OnEnter();
			
			// TODO: Do animated swapping instead of the one below.
			ObjectHelpers.TransformInfo temp;
			temp.position = Vector3.zero;
			temp.rotation = Quaternion.identity;
			temp.scale = Vector3.one;
			ObjectHelpers.CopyTransform( parent.m_mouseDownTile.transform, ref temp );
			ObjectHelpers.CopyTransform( parent.m_mouseUpTile.transform, parent.m_mouseDownTile.transform );
			ObjectHelpers.CopyTransform( ref temp, parent.m_mouseUpTile.transform );
			
			fsm.TransitionTo< EvaluteGrid >();
		}
	}
}
