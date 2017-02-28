using System;
using System.Collections.Generic;
using System.Linq;

namespace Halite
{
    public class Map
    {
        public void Update(string gameMapStr)
        {
            var gameMapValues = new Queue<string>(gameMapStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            ushort x = 0, y = 0;
            while (y < Height)
            {
                ushort counter, owner;
                if (!ushort.TryParse(gameMapValues.Dequeue(), out counter))
                    throw new ApplicationException("Could not get some counter from stdin");
                if (!ushort.TryParse(gameMapValues.Dequeue(), out owner))
                    throw new ApplicationException("Could not get some owner from stdin");
                while (counter > 0)
                {
                    _sites[x, y].Owner = owner;
                    x++;
                    if (x == Width)
                    {
                        x = 0;
                        y++;
                    }
                    counter--;
                }
            }

            var strengthValues = gameMapValues; 
            for (y = 0; y < Height; y++)
            {
                for (x = 0; x < Width; x++)
                {
                    ushort strength;
                    if (!ushort.TryParse(strengthValues.Dequeue(), out strength))
                        throw new ApplicationException("Could not get some strength value from stdin");
                    _sites[x, y].Strength = strength;
                }
            }
        }

        public List<Site> GetAllSites()
        {
            return _sitesList.ToList();
        }

        public List<Site> GetSites(Func<Site, bool> filter)
        {
            return _sitesList.Where(filter).ToList();
        }

        public int SiteCount => Height * Width;

        public Site this[int x, int y] => _sites[x.Mod(Config.Get().MapWidth), y.Mod(Config.Get().MapHeight)];

        public ushort Width => (ushort)_sites.GetLength(0);
        public ushort Height => (ushort)_sites.GetLength(1);
        public int Area => Height * Width;
        public int NumPlayers => GetSites(x => !x.IsNeutral).Select(x => x.Owner).Distinct().Count();
        public int AreaPerPlayer => (int)(Area / NumPlayers);

        public List<Site> GetMySites()
        {
            return _sitesList.Where(s => s.Owner == Config.Get().PlayerTag).ToList();
        }

        #region Implementation

        private readonly Site[,] _sites;
        private readonly List<Site> _sitesList;

        private Map(int width, int height)
        {
            _sites = new Site[width, height];
            _sitesList = new List<Site>();

            for (ushort x = 0; x < width; x++)
            {
                for (ushort y = 0; y < height; y++)
                {
                    _sites[x, y] = new Site(x, y);
                    _sitesList.Add(_sites[x, y]);
                }
            }

            foreach (var site in _sitesList)
            {
                //populate neighbours
                var top = _sites[site.X, (site.Y - 1).Mod(height)];
                var bottom = _sites[site.X, (site.Y + 1).Mod(height)];
                var left = _sites[(site.X - 1).Mod(width), site.Y];
                var right = _sites[(site.X + 1).Mod(width), site.Y];

                site.PopulateNeighbours(top, bottom, left, right);
            }
        }

        private static void ParseMapSize(string mapSizeStr, out int width, out int height)
        {
            var parts = mapSizeStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[0], out width) || !int.TryParse(parts[1], out height))
                throw new ApplicationException("Could not get map size from stdin during init");
        }

        public static Map ParseMap(string mapSizeStr, string productionMapStr, string gameMapStr, int playerTag)
        {
            int width, height;
            ParseMapSize(mapSizeStr, out width, out height);
            var map = new Map(width, height);

            var productionValues = new Queue<string>(productionMapStr.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));

            ushort x, y;
            for (y = 0; y < map.Height; y++)
            {
                for (x = 0; x < map.Width; x++)
                {
                    ushort production;
                    if (!ushort.TryParse(productionValues.Dequeue(), out production))
                        throw new ApplicationException("Could not get some production value from stdin");
                    map._sites[x, y].Production = production;
                }
            }

            Config.Init(playerTag, width, height);

            map.Update(gameMapStr);

            return map;
        }

        #endregion

    }
}
