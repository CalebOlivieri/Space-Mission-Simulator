using System;
using System.Collections.Generic;

namespace SpaceMissionSimulator.Models
{
    public static class MissionGenerator
    {
        private static Random _rnd = new Random();

        // Generate simple gather missions for available planets
        public static IEnumerable<Mission> GenerateGatherMissions(IEnumerable<Planet> planets, int count = 3)
        {
            var list = new List<Mission>();
            var planetArray = new List<Planet>(planets);
            if (planetArray.Count == 0) return list;

            for (int i = 0; i < count; i++)
            {
                var p = planetArray[_rnd.Next(planetArray.Count)];
                int amt = Math.Min(p.ResourceAmount, 10 + _rnd.Next(40));
                if (amt <= 0) continue;
                var m = new Mission
                {
                    Type = MissionType.Gather,
                    Description = $"Recolectar {amt} {p.ResourceType} de {p.Name}",
                    From = p.Parent ?? p,
                    To = p,
                    ResourceType = p.ResourceType,
                    ResourceAmount = amt,
                    RewardCredits = amt * (10 + _rnd.Next(20))
                };
                list.Add(m);
            }

            return list;
        }
    }
}
