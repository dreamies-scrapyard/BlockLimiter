using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using BlockLimiter.Settings;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.Graphics.GUI;
using VRage.Game;

namespace BlockLimiter.Utility
{
    public static class Grid
    {
        public static bool IsSizeViolation(long id)
        {
            return GridCache.TryGetGridById(id, out var grid) && IsSizeViolation(grid);
        }

        public static bool IsSizeViolation(MyObjectBuilder_CubeGrid grid)
        {

            if (grid == null)
            {
                return false;
            }
            var gridSize = grid.CubeBlocks.Count;
            var gridType = grid.GridSizeEnum;
            var isStatic = grid.IsStatic;

            if (BlockLimiterConfig.Instance.MaxBlockSizeShips > 0 && !isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeShips)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlockSizeStations > 0 && isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeStations)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksLargeGrid > 0 && gridType == MyCubeSize.Large && gridSize >= BlockLimiterConfig.Instance.MaxBlocksLargeGrid)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksSmallGrid > 0 && gridType == MyCubeSize.Small && gridSize >= BlockLimiterConfig.Instance.MaxBlocksSmallGrid)
            {
                return  true;
            }

            return false;
        }

        public static bool IsSizeViolation(MyCubeGrid grid, bool converting = false)
        {
            if (grid == null) return false;


            if (grid.EntityId > 0 && Utilities.IsExcepted(grid.EntityId, new List<string>()) || grid.BigOwners.Any(x => Utilities.IsExcepted(x, new List<string>())))
                return false;

            var gridSize = grid.CubeBlocks?.Count;
            var gridType = grid.GridSizeEnum;
            var isStatic = converting? !grid.IsStatic:grid.IsStatic;

            if (BlockLimiterConfig.Instance.MaxBlockSizeShips > 0 && !isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeShips)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlockSizeStations > 0 && isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeStations)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksLargeGrid > 0 && gridType == MyCubeSize.Large && gridSize >= BlockLimiterConfig.Instance.MaxBlocksLargeGrid)
            {
                return  true;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksSmallGrid > 0 && gridType == MyCubeSize.Small && gridSize >= BlockLimiterConfig.Instance.MaxBlocksSmallGrid)
            {
                return  true;
            }

            return false;
        }

        public static bool CanMerge(MyCubeGrid grid1, MyCubeGrid grid2)
        {
            if (grid1 == null || grid2 == null) return true;

            if (grid1.BigOwners.Any(x => Utilities.IsExcepted(x, new List<string>())) ||
                grid2.BigOwners.Any(x => Utilities.IsExcepted(x, new List<string>()))) return true;
            
            var blocksHash = new HashSet<MySlimBlock>(grid1.CubeBlocks);
            
            blocksHash.UnionWith(grid2.CubeBlocks);

            if (blocksHash.Count == 0) return true;
            
            var gridSize = grid1.CubeBlocks.Count + grid2.CubeBlocks.Count;
            var gridType = grid1.GridSizeEnum;
            var isStatic = grid1.IsStatic || grid2.IsStatic;

            if (BlockLimiterConfig.Instance.MaxBlockSizeShips > 0 && !isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeShips)
            {
                return  false;
            }

            if (BlockLimiterConfig.Instance.MaxBlockSizeStations > 0 && isStatic && gridSize >= BlockLimiterConfig.Instance.MaxBlockSizeStations)
            {
                return  false;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksLargeGrid > 0 && gridType == MyCubeSize.Large && gridSize >= BlockLimiterConfig.Instance.MaxBlocksLargeGrid)
            {
                return  false;
            }

            if (BlockLimiterConfig.Instance.MaxBlocksSmallGrid > 0 && gridType == MyCubeSize.Small && gridSize >= BlockLimiterConfig.Instance.MaxBlocksSmallGrid)
            {
                return  false;
            }


            foreach (var limit in BlockLimiterConfig.Instance.AllLimits)
            {
                if (!limit.LimitGrids) continue;

                if (Utilities.IsExcepted(grid1.EntityId, limit.Exceptions)|| Utilities.IsExcepted(grid2.EntityId, limit.Exceptions)) continue;
                
                if (blocksHash.Count(x=> Block.IsMatch(x.BlockDefinition,limit)) <= limit.Limit) continue;
                return false;
            }

            return true;

        }

        public static bool AllowConversion(MyCubeGrid grid)
        {
            if (IsSizeViolation(grid,true)) return false;

            foreach (var limit in BlockLimiterConfig.Instance.AllLimits)
            {
                if (!limit.LimitGrids) continue;
                switch (limit.GridTypeBlock)
                {
                    case LimitItem.GridType.ShipsOnly when !grid.IsStatic:
                    case LimitItem.GridType.StationsOnly when grid.IsStatic:
                    case LimitItem.GridType.AllGrids:
                    case LimitItem.GridType.SmallGridsOnly:
                    case LimitItem.GridType.LargeGridsOnly:
                        continue;
                }
                
                if (grid.BigOwners.Count > 0 && grid.BigOwners.Any(x=> Utilities.IsExcepted(x, limit.Exceptions))) continue;


                var count = grid.CubeBlocks.Count(x => Block.IsMatch(x.BlockDefinition, limit));


                if (count <= limit.Limit)
                {
                    continue;
                }

                return false;

            }

            return true;
        }
        
        public static bool IsGridType(MyCubeGrid grid, LimitItem item)
        {
            switch (item.GridTypeBlock)
            {
                case LimitItem.GridType.SmallGridsOnly:
                    return grid.GridSizeEnum == MyCubeSize.Small;
                case LimitItem.GridType.LargeGridsOnly:
                    return grid.GridSizeEnum == MyCubeSize.Large;
                case LimitItem.GridType.StationsOnly:
                    return grid.IsStatic;
                case LimitItem.GridType.ShipsOnly:
                    return !grid.IsStatic;
                case LimitItem.GridType.AllGrids:
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsGridType(MyObjectBuilder_CubeGrid grid, LimitItem item)
        {
            switch (item.GridTypeBlock)
            {
                case LimitItem.GridType.AllGrids:
                    return true;
                case LimitItem.GridType.SmallGridsOnly:
                    return grid.GridSizeEnum == MyCubeSize.Small;
                case LimitItem.GridType.LargeGridsOnly:
                    return grid.GridSizeEnum == MyCubeSize.Large;
                case LimitItem.GridType.StationsOnly:
                    return grid.IsStatic;
                case LimitItem.GridType.ShipsOnly:
                    return !grid.IsStatic;
                default:
                    return false;
            }
        }


        public static bool CanSpawn(MyObjectBuilder_CubeGrid grid, long playerId)
        {
            if (Utilities.IsExcepted(playerId, new List<string>())) return true;

            if (grid == null || IsSizeViolation(grid)) return false;

            return playerId == 0 || Block.CanAdd(grid.CubeBlocks, playerId, out _);
        }


    }
}