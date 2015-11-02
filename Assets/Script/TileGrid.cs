/// <summary>
/// Unity Match3 Tile Grid class
/// Author: Andrei Victor (me@andreivictor.net)
/// https://www.github.com/elmachotroso/
///
/// Tile grid - is a basic all-in-one core logic of a match3 game. This is
/// currently implemented as a turn-based approach for simplicity but can
/// easily be extended or modified to cater for a more interactive version.
/// </summary>

using UnityEngine;
using System.Collections.Generic;

// The TileGrid class manages an m x n grid of tiles, which lets you swap two
// adjacent tiles and check for matching tiles and gain a scoring information
// from it.
// Usage:
public class TileGrid
{
    // The enumerated tile types to be associated in the grid.
    public enum TileTypes
    {
        None    = 0,
        First   = 1,
        Red     = 1,
        Yellow,
        Green,
        Blue,
        Purple,
        Max
    }
    
    // The data structure of a tile along with its properties. A grid is
    // composed of ordered TileData.
    public class TileData
    {
        public bool m_isMatched = false;            // flag to mark if the tile has been matched.
        public bool m_isChecked = false;            // flag to mark if the tile has been checked for matching.
        public TileTypes m_type = TileTypes.None;   // the type of the tile that makes it distinguishable to other tiles.
    }
    
    // Method that simply swaps two tiles.
    public void SwapTile( int index, int index2 )
    {
        if( index < 0 || index >= m_grid.Length
            || index2 < 0 || index2 >= m_grid.Length )
        {
            return;
        }
        
        TileData tileTemp = m_grid[ index ];
        m_grid[ index ] = m_grid[ index2 ];
        m_grid[ index2 ] = tileTemp;        
    }
    
    // Constructor of the tile grid given the number of tiles via width and
    // height, where width and height should at least be 3. This automatically
    // calls the InitializeGrid method.
    public TileGrid( int width, int height )
    {
        InitializeGrid( width, height );
    }
    
    // Initializes (reinitializes the grid) given the number of tiles via width
    // and height, where width and height should at least be 3. It also fills up
    // the grid with randomized tiles in the TileTypes enumeration.
    public void InitializeGrid( int width, int height )
    {
        width = ( width < 3 ) ? 3 : width;
        height = ( height < 3 ) ? 3 : height;
        m_width = width;
        m_height = height;
        
        do
        {
            m_grid = null; // release previous grid
            int gridSize = m_width * m_height;
            m_grid = new TileData[ gridSize ];
            for( int i = 0; i < gridSize; ++i )
            {
                TileData tile = new TileData();
                m_grid[ i ] = tile;
                tile.m_type = (TileTypes) Random.Range( (int) TileTypes.First, (int) TileTypes.Max );
            }
            
            DoGridAlgorithmStep( false ); // Simulate grid algorithms to properly clean initial matches.
        }
        while( !IsGridSolvable() );        
    }
    
    // This method simulates all match-making and gravity applying of new
    // files being generated.
    // @param applyScore - [optional] if true, it compute the matches and number
    // of chains.
    // @param matchCount - [out] the variable contains the number of tiles
    // matched all througout the simulation.
    // @param chainCount - [out] the variable contains the number of consecutive
    // chains occured.
    public void DoGridAlgorithmStep( bool applyScores = true )
    {
        int temp1 = 0, temp2 = 0;
        DoGridAlgorithmStep( out temp1, out temp2, applyScores );
    }
    
    // Refer to above description.
    public void DoGridAlgorithmStep( out int matchCount, out int chainCount, bool applyScores = true )
    {
        int matches = 0;
        int chains = -1; // first matches don't count as a chain.
        matchCount = matches;
        chainCount = 0;
        do
        {
            DoMatchChecking();
            matches = DoClaimMatches();
            if( applyScores )
            {
                matchCount += matches;
                if( matches > 0 )
                {
                    ++chains;
                    chainCount = chains;
                }
            }
            DoUnmarkAllTiles();
            DoTileMovements();
        } while( matches > 0 );
    }
    
    // This resets the tile's TileData properties to their defaults. Match and
    // checked flags will be both false and the type will be None if type
    // parameter is not specified.
    public void ResetTile( int x, int y, TileTypes type = TileTypes.None )
    {
        ResetTile( GetTileIndex( x, y ), type );
    }
    
    // Refer to above description.
    public void ResetTile( int index, TileTypes type = TileTypes.None )
    {
        TileData tile = GetTile( index );
        if( tile != null )
        {
            tile.m_isChecked = false;
            tile.m_isMatched = false;
            tile.m_type = type;
        }
    }
    
    // Retrieves the TileData for the tile located in the x, y grid. Otherwise,
    // it returns null for invalid coordinates or missing.
    public TileData GetTile( int x, int y )
    {
        return GetTile( GetTileIndex( x, y ) );
    }
    
    // Refer to above description.
    public TileData GetTile( int index )
    {
        if( index < 0 || index >= m_grid.Length )
        {
            return null;
        }
        
        return m_grid != null ? m_grid[ index ] : null;
    }
    
    // A function that simply transforms a grid coordinate x and y into a single
    // tile index value. (The grid is actually a one-dimensional array).
    public int GetTileIndex( int x, int y )
    {
        if( x < 0 || x >= m_width || y < 0 || y >= m_height )
        {
            return INVALID_INDEX;
        }
        
        return y * m_width + x;
    }
    
    // Return the tile index given the tile data.
    public int GetTileIndex( TileData tileData )
    {
    	int index = INVALID_INDEX;
    	for( int i = 0; i < m_grid.Length; ++i )
    	{
    		if( m_grid[ i ] == tileData )
    		{
    			index = i;
    			break;
    		}
    	}
    	
    	return index;
    }
    
    // Retrieve the x, and y coords of the given tile data.
    public Vector2 GetTileCoords( TileData tileData )
    {
    	Vector2 coords = new Vector2( INVALID_INDEX, INVALID_INDEX );
    	int index = GetTileIndex( tileData );
    	if( index != INVALID_INDEX )
    	{
    		coords = new Vector2( index % m_width, index / m_width );
    	}
    	
    	return coords;
    }
    
    // Returns true if the specified tiles in tileIndex1 and tileIndex2 are
    // besides each other either horizontally or vertically.
    public bool IsTileAdjacent( int tileIndex1, int tileIndex2 )
    {
        int y = tileIndex1 / m_width;
        int x = tileIndex1 % m_width;
        return ( tileIndex2 == GetTileIndex( x - 1, y )
           || tileIndex2 == GetTileIndex( x + 1, y )
           || tileIndex2 == GetTileIndex( x, y - 1 )
           || tileIndex2 == GetTileIndex( x, y + 1 ) );
    }
    
    // Returns true if the grid has any tiles marked as matched. This is only
    // applicable when called after match checking and before resetting.
    public bool IsGridHaveMatch()
    {
        int gridSize = m_width * m_height;
        for( int i = 0; i < gridSize; ++i )
        {
            if( m_grid[ i ].m_isMatched )
            {
                return true;
            }
        }
        return false;
    }
    
    // Returns true if the grid has any solutions available, which means some
    // tiles can be swapped to resolve the grid.
    public bool IsGridSolvable()
    {
        List< int > solutions = GetPossibleSolutions();
        return solutions != null && solutions.Count > 0;
    }
    
    // Returns the list of possible tiles that can be swapped to be able to
    // resolve a match. This is useful when wanting to show hints to the player.
    public List< int > GetPossibleSolutions()
    {
        List< int > solutions = new List< int >();
        
        int gridSize = m_grid.Length;
        for( int i = 0; i < gridSize; ++i )
        {
            TileData tile = m_grid[ i ];
            if( tile == null )
            {
                Debug.LogError( "Skipped an empty tile!" );
                continue;
            }
            
            int y = i / m_width;
            int x = i % m_width;
            
            // Try swap with up.
            int tileIndexUp = GetTileIndex( x, y - 1 );
            if( tileIndexUp != INVALID_INDEX )
            {
                SwapTile( i, tileIndexUp ); // swap
                bool match = IsMatchingAnyAdjacentTiles( i );
                SwapTile( i, tileIndexUp ); // unswap
                if( match )
                {
                    solutions.Add( tileIndexUp );
                    //continue;
                }
            }
            
            // Try swap with down.
            int tileIndexDown = GetTileIndex( x, y + 1 );
            if( tileIndexDown != INVALID_INDEX )
            {
                SwapTile( i, tileIndexDown ); // swap
                bool match = IsMatchingAnyAdjacentTiles( i );
                SwapTile( i, tileIndexDown ); // unswap
                if( match )
                {
                    solutions.Add( tileIndexDown );
                    //continue;
                }
            }
            
            // Try swap with left.
            int tileIndexLeft = GetTileIndex( x - 1, y );
            if( tileIndexLeft != INVALID_INDEX )
            {
                SwapTile( i, tileIndexLeft ); // swap
                bool match = IsMatchingAnyAdjacentTiles( i );
                SwapTile( i, tileIndexLeft ); // unswap
                if( match )
                {
                    solutions.Add( tileIndexLeft );
                    //continue;
                }
            }
            
            // Try swap with right.
            int tileIndexRight = GetTileIndex( x + 1, y );
            if( tileIndexRight != INVALID_INDEX )
            {
                SwapTile( i, tileIndexRight ); // swap
                bool match = IsMatchingAnyAdjacentTiles( i );
                SwapTile( i, tileIndexRight ); // unswap
                if( match )
                {
                    solutions.Add( tileIndexRight );
                    //continue;
                }
            }
        }
        
        return solutions;
    }
    
    // Checks the grid for matches and marks their TileData flags properly.
    public void DoMatchChecking()
    {
        int gridSize = m_grid.Length;
        for( int i = 0; i < gridSize; ++i )
        {
            TileData tile = m_grid[ i ];
            if( tile != null && !tile.m_isChecked )
            {
                int y = i / m_width;
                int x = i % m_width;
                CheckAdjacentTilesForMatch( x, y );
            }
        }
    }
    
    // Performs the claiming of the marked matched tiles. This tiles matched
    // are counted and reset to none. This is ideal place to trigger on matched
    // events.
    public int DoClaimMatches()
    {
        int tilesMatched = 0;
        int gridSize = m_grid.Length;
        for( int i = 0; i < gridSize; ++i )
        {
            TileData tile = m_grid[ i ];
            if( tile != null && tile.m_isMatched )
            {
                ++tilesMatched;
                tile.m_type = TileTypes.None;
                tile.m_isMatched = false;
                tile.m_isChecked = false;
            }
        }
        
        return tilesMatched;
    }
    
    // A full reset on the properties except the type of the grid's tiles.
    public void DoUnmarkAllTiles()
    {
        int gridSize = m_grid.Length;
        for( int i = 0; i < gridSize; ++i )
        {
            TileData tile = m_grid[ i ];
            if( tile != null )
            {
                tile.m_isMatched = false;
                tile.m_isChecked = false;
            }
        }
    }
    
    // Performs the gravity step successively until there are no more tiles that
    // need to fall down due to gravity. This function uses ApplyTileMovements()
    // over and over while there are still holes.
    public void DoTileMovements()
    {
        while( ApplyTileMovements() > 0 );
    }
    
    // Performs ONE grid gravity step. It starts from the lowest part of the
    // grid and swapping tiles upward to simulate the tile falling down one tile
    // and when reaching an empty top, it regenerates a random tile.
    // It returns the number of holes present in the grid after performing the
    // step. This is useful in determining if there are still tiles needed to
    // apply the gravity.
    protected int ApplyTileMovements()
    {
        int holesInGrid = 0;
        for( int i = m_grid.Length - 1; i >= 0; --i )
        {
            TileData tile = m_grid[ i ];
            if( tile != null && tile.m_type == TileTypes.None )
            {
                int y = i / m_width;
                int x = i % m_width;
                if( y - 1 < 0 )
                {
                    tile.m_type = (TileTypes) Random.Range( (int) TileTypes.First, (int) TileTypes.Max );
                }
                else
                {
                    SwapTile( i, GetTileIndex( x, y - 1 ) );
                    TileData newTile = GetTile( i ); // Get the new tile after swapping.
                    if( newTile != null && newTile.m_type == TileTypes.None )
                    {
                        ++holesInGrid;
                    }
                }
            }
        }
        
        return holesInGrid;
    }
    
    // Refer to the description below.
    public bool IsMatchingAnyAdjacentTiles( int x, int y )
    {
        return IsMatchingAnyAdjacentTiles( GetTileIndex( x, y ) );
    }
    
    // Returns true if the specified tile in the grid has a valid match. Note
    // that this version is quite expensive because this has to perform further
    // additional match check on the 2nd level adjacent tiles because this does
    // not traverse the whole grid.
    // This version is used by GetPossibleSolutions().
    public bool IsMatchingAnyAdjacentTiles( int index )
    {
        if( index < 0 || index > m_grid.Length )
        {
            // out of bounds
            return false;
        }
        
        int y = index / m_width;
        int x = index % m_width;
        
        TileData thisTile = m_grid[ index ];
        TileData aboveTile = GetTile( x, y - 1 );
        TileData belowTile = GetTile( x, y + 1 );
        TileData leftTile = GetTile( x - 1, y );
        TileData rightTile = GetTile( x + 1, y );
        
        if( aboveTile != null && aboveTile.m_type == thisTile.m_type
           && belowTile != null && belowTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        if( leftTile != null && leftTile.m_type == thisTile.m_type
           && rightTile != null && rightTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        // IsMatchingAnyAdjacentTiles is a special case because it has to look
        // at the third block level block
        TileData aboveTile2 = GetTile( x, y - 2 );
        TileData belowTile2 = GetTile( x, y + 2 );
        TileData leftTile2 = GetTile( x - 2, y );
        TileData rightTile2 = GetTile( x + 2, y );
        
        if( aboveTile2 != null && aboveTile2.m_type == thisTile.m_type
           && aboveTile != null && aboveTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        if( belowTile2 != null && belowTile2.m_type == thisTile.m_type
           && belowTile != null && belowTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        if( leftTile2 != null && leftTile2.m_type == thisTile.m_type
           && leftTile != null && leftTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        if( rightTile2 != null && rightTile2.m_type == thisTile.m_type
           && rightTile != null && rightTile.m_type == thisTile.m_type )
        {
            return true;
        }
        
        return false;
    }
    
    // Refer to the description below.
    protected void CheckAdjacentTilesForMatch( int x, int y )
    {
        CheckAdjacentTilesForMatch( GetTileIndex( x, y ) );
    }
        
    // This method traverses each tile in the grid and performs a simpler check
    // for matches (only the first tiles adjacent to the tile) and marks them
    // as matched if it satisfies the condition. This relies on the fact that
    // the checking is performed on all tiles so the 2nd level check is
    // unnecessary.
    protected void CheckAdjacentTilesForMatch( int index )
    {
        if( index < 0 || index > m_grid.Length )
        {
            // out of bounds
            return;
        }
        
        int y = index / m_width;
        int x = index % m_width;
        
        TileData thisTile = m_grid[ index ];
        thisTile.m_isChecked = true;
        TileData aboveTile = GetTile( x, y - 1 );
        TileData belowTile = GetTile( x, y + 1 );
        TileData leftTile = GetTile( x - 1, y );
        TileData rightTile = GetTile( x + 1, y );
        
        if( aboveTile != null && aboveTile.m_type == thisTile.m_type
            && belowTile != null && belowTile.m_type == thisTile.m_type )
        {
            thisTile.m_isMatched = true;
            aboveTile.m_isMatched = true;
            belowTile.m_isMatched = true;
        }
        
        if( leftTile != null && leftTile.m_type == thisTile.m_type
            && rightTile != null && rightTile.m_type == thisTile.m_type )
        {
            thisTile.m_isMatched = true;
            leftTile.m_isMatched = true;
            rightTile.m_isMatched = true;
        }
    }
    
    public static int INVALID_INDEX         = -1;   // Invalid tile index marker.
    protected int m_width                   = 7;
    protected int m_height                  = 6;
    protected TileData[] m_grid             = null;
}
