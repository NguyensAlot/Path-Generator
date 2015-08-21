using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mapGen3
{
    public enum TileTypes
    {
        EndPoint,           // 0
        StartPoint,         // 1
        HorizPath,          // 2
        VertPath,           // 3
        ElbowLeftUp,        // 4
        ElbowRightUp,       // 5
        ElbowLeftDown,      // 6
        ElbowRightDown,     // 7
        SplitULR,           // 8
        SplitUDR,           // 9
        SplitDLR,           //10
        SplitUDL,           //11
        Split4Ways,         //12
        EmptySpace,         //13
        CurPosition,        //14
        Decor,              //15
        Tower               //16
    };

    public enum MapDifficulty
    {
        Small = 10, Medium = 15, Large = 20
    };

    public enum MapLength
    {
        Short = 0, Medium, Long
    };
}

//private static bool HasAdjacentTile(int y, int x, TileType tile)
//{
//    // check if path tiles are adjacent to each other
//    switch (tile)
//    {
//        // Split4Ways is allowed to have tiles on all sides
//        case TileType.Split4Ways:
//            return false;
//        // SplitUDL not allowed to have tile to the right
//        case TileType.SplitUDL:
//            if (x < GRID_WIDTH - 1 && grid[y, x + 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // SplitUDR not allowed to have tile to the left*
//        case TileType.SplitUDR:
//            if (x > 0 && grid[y, x - 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // SplitULR not allowed to have tile to the bottom
//        case TileType.SplitULR:
//            if (y > 0 && grid[y - 1, x].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // SplitDLR not allowed to have tile to the top
//        case TileType.SplitDLR:
//            if (y < GRID_HEIGHT - 1 && grid[y + 1, x].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // ElbowLeftUp not allowed to have tile to the bottom or right
//        case TileType.ElbowLeftUp:
//            if (y > 0 && grid[y - 1, x].tile < TileType.EmptySpace)
//                return true;
//            if (x < GRID_WIDTH - 1 && grid[y, x + 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // ElbowRightUp not allowed to have tile to the bottom or left
//        case TileType.ElbowRightUp:
//            if (y > 0 && grid[y - 1, x].tile < TileType.EmptySpace)
//                return true;
//            if (x < GRID_WIDTH - 1 && grid[y, x - 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // ElbowLeftDown not allowed to have tile to the top or right
//        case TileType.ElbowLeftDown:
//            if (y < GRID_HEIGHT - 1 && grid[y + 1, x].tile < TileType.EmptySpace)
//                return true;
//            if (x < GRID_WIDTH - 1 && grid[y, x + 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // ElbowRightDown not allowed to have tile to the top or left
//        case TileType.ElbowRightDown:
//            if (y < GRID_HEIGHT - 1 && grid[y + 1, x].tile < TileType.EmptySpace)
//                return true;
//            if (x > 0 && grid[y, x - 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // HorizPath not allowed to have tile to the top or bottom
//        case TileType.HorizPath:
//            if (y > 0 && grid[y - 1, x].tile < TileType.EmptySpace)
//                return true;
//            if (y < GRID_HEIGHT - 1 && grid[y + 1, x].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        // VertPath not allowed to have tile to the left or right
//        case TileType.VertPath:
//            if (x > 0 && grid[y, x - 1].tile < TileType.EmptySpace)
//                return true;
//            if (x < GRID_WIDTH - 1 && grid[y, x + 1].tile < TileType.EmptySpace)
//                return true;
//            return false;
//        default:
//            return false;
//    }
//}



//switch (tile)
//{
//    // Split4Ways is allowed to have tiles on all sides
//    case TileType.Split4Ways:
//        return false;
//    // SplitUDL not allowed to have tile to the right
//    case TileType.SplitUDL:
//        if (x < GRID_WIDTH - 1 && (grid[y, x + 1].tile == TileType.EmptySpace || grid[y, x + 1].tile == TileType.Tower))
//            return false;
//        return true;
//    // SplitUDR not allowed to have tile to the left
//    case TileType.SplitUDR:
//        if (x > 0 && (grid[y, x - 1].tile == TileType.EmptySpace || grid[y, x - 1].tile == TileType.Tower))
//            return false;
//        return true;
//    // SplitULR not allowed to have tile to the bottom
//    case TileType.SplitULR:
//        if (y < GRID_HEIGHT - 1 && (grid[y + 1, x].tile == TileType.EmptySpace || grid[y + 1, x].tile == TileType.Tower))
//            return false;
//        return true;
//    // SplitDLR not allowed to have tile to the top
//    case TileType.SplitDLR:
//        if (y > 0 && (grid[y - 1, x].tile == TileType.EmptySpace || grid[y - 1, x].tile == TileType.Tower))
//            return false;
//        return true;
//    // ElbowLeftUp not allowed to have tile to the bottom and right
//    case TileType.ElbowLeftUp:
//        if (y < GRID_HEIGHT - 1 && (grid[y + 1, x].tile == TileType.EmptySpace || grid[y + 1, x].tile == TileType.Tower))
//            if (x < GRID_WIDTH - 1 && (grid[y, x + 1].tile == TileType.EmptySpace || grid[y, x + 1].tile == TileType.Tower))
//                return false;
//        return true;
//    // ElbowRightUp not allowed to have tile to the bottom and left
//    case TileType.ElbowRightUp:
//        if (y < GRID_HEIGHT - 1 && (grid[y + 1, x].tile == TileType.EmptySpace || grid[y + 1, x].tile == TileType.Tower))
//            if (x > 0 && (grid[y, x - 1].tile == TileType.EmptySpace || grid[y, x - 1].tile == TileType.Tower))
//                return false;
//        return true;
//    // ElbowLeftDown not allowed to have tile to the top and right
//    case TileType.ElbowLeftDown:
//        if (y > 0 && (grid[y - 1, x].tile == TileType.EmptySpace || grid[y - 1, x].tile == TileType.Tower))
//            if (x < GRID_WIDTH - 1 && (grid[y, x + 1].tile == TileType.EmptySpace || grid[y, x + 1].tile == TileType.Tower))
//                return false;
//        return true;
//    // ElbowRightDown not allowed to have tile to the top and left
//    case TileType.ElbowRightDown:
//        if (y > 0 && (grid[y - 1, x].tile == TileType.EmptySpace || grid[y - 1, x].tile == TileType.Tower))
//            if (x > 0 && (grid[y, x - 1].tile == TileType.EmptySpace || grid[y, x - 1].tile == TileType.Tower))
//                return false;
//        return true;
//    // HorizPath not allowed to have tile to the top and bottom
//    case TileType.HorizPath:
//        if (y < GRID_HEIGHT - 1 && (grid[y + 1, x].tile == TileType.EmptySpace || grid[y + 1, x].tile == TileType.Tower))
//            if (y > 0 && (grid[y - 1, x].tile == TileType.EmptySpace || grid[y - 1, x].tile == TileType.Tower))
//                return false;
//        return true;
//    // VertPath not allowed to have tile to the left and right
//    case TileType.VertPath:
//        if (x > 0 && (grid[y, x - 1].tile == TileType.EmptySpace || grid[y, x - 1].tile == TileType.Tower))
//            if (x < GRID_WIDTH - 1 && (grid[y, x + 1].tile == TileType.EmptySpace || grid[y, x + 1].tile == TileType.Tower))
//                return false;
//        return true;
//    default:
//        return false;
//}