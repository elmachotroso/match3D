/// <summary>
/// Unity Match3 Tile Grid Test Case component class
/// Author: Andrei Victor (me@andreivictor.net)
/// https://www.github.com/elmachotroso/
///
/// Just a test case for a basic implementation of the match-3 game using the
/// TileGrid class. This demonstrates the proper calls and assumptions to
/// simulate this game mechanic.
/// </summary>

using UnityEngine;
using System.Collections.Generic;

public class TileGridTestCase : MonoBehaviour
{
	protected void Awake()
	{
		m_gridScreenPos = m_screenOffset;
		if( m_centerGridScreen )
		{
			m_gridScreenPos.x += ( ( Screen.width - ( m_gridWidth * TILE_SIZE ) ) / 2 );
			m_gridScreenPos.y += ( ( Screen.height - ( m_gridHeight * TILE_SIZE ) ) / 2 );
		}
		
		m_grid = new TileGrid( m_gridWidth, m_gridHeight );
		m_lastSeenSolutions = m_grid.GetPossibleSolutions();
		if( m_grid == null )
		{
			Debug.LogError( "Failed to create TileGrid." );
			enabled = false;
			return;
		}
	}
	
	protected void OnGUI()
	{
		if( m_grid == null )
		{
			return;
		}
		
		Color defBgColor = GUI.backgroundColor;
		for( int x = 0; x < m_gridWidth; ++x )
		{
			for( int y = 0; y < m_gridHeight; ++y )
			{
				int tileIndex = m_grid.GetTileIndex( x, y );				
				TileGrid.TileTypes type = m_grid.GetTile( tileIndex ).m_type;
				GUI.backgroundColor = ( m_lastSelected == tileIndex ) ? Color.white : TileTypeToColor( type );
				GUI.contentColor = ( m_lastSelected == tileIndex ) ? Color.black : Color.white;
				if( m_lastSeenSolutions != null && m_lastSeenSolutions.Contains( tileIndex ) )
				{
					GUI.contentColor = Color.cyan;
				}
				
				if( GUI.Button( new Rect( m_gridScreenPos.x + ( x * TILE_SIZE ),
                    m_gridScreenPos.y + ( y * TILE_SIZE ),
                    TILE_SIZE, TILE_SIZE ),
					type.ToString() ) )
				{
					if( m_lastSelected < 0 )
					{
						m_lastSelected = tileIndex;
					}
					else if( m_grid.IsTileAdjacent( m_lastSelected, tileIndex ) )
					{
						int matches = 0;
						int chains = 0;
						m_grid.SwapTile( m_lastSelected, tileIndex );
						m_grid.DoGridAlgorithmStep( out matches, out chains );
						if( matches > 0 )
						{
							// apply scores
							Debug.LogWarningFormat( "Matches: {0}\n Chains: {1}", matches, chains );
							
							m_lastSeenSolutions = m_grid.GetPossibleSolutions();
							Debug.Log( string.Format( "There are {0} possible solutions.", m_lastSeenSolutions.Count ) );
						}
						else
						{
							Debug.LogWarning( "No matches." );
							m_grid.SwapTile( m_lastSelected, tileIndex );
						}
						m_lastSelected = -1;
					}
				}
			}
		}
		GUI.backgroundColor = defBgColor;
	}
	
	protected Color TileTypeToColor( TileGrid.TileTypes type )
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
    
    public static int TILE_SIZE                         = 50;
	
	[SerializeField] protected Vector2 m_screenOffset  	= new Vector2( 0.0f, 0.0f );
	[SerializeField] protected bool m_centerGridScreen	= true;
	[SerializeField] protected int m_gridWidth			= 7;
	[SerializeField] protected int m_gridHeight			= 6;
	private List< int > m_lastSeenSolutions				= null;
	private TileGrid m_grid								= null;
	private int m_lastSelected							= -1;
	private Vector2 m_gridScreenPos						= new Vector2( 0.0f, 0.0f );
}
