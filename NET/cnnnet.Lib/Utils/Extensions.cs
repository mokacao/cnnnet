﻿using cnnnet.Lib.Neurons;
using System;
using System.Collections.Generic;
using System.ComponentModel;

using System.Linq;

namespace cnnnet.Lib.Utils
{
    public static class Extensions
    {
        public static double GetDistance(int x1, int y1, int x2, int y2)
        {
            return x1 == x2 && y1 == y2
                ? 0
                : Math.Sqrt(Math.Pow(x1 - x2, 2) + Math.Pow(y1 - y2, 2));
        }

        public static void InvokeEx<T>(this T @this, Action<T> action) where T : ISynchronizeInvoke
        {
            if (@this.InvokeRequired)
            {
                @this.Invoke(action, new object[] { @this });
            }
            else
            {
                action(@this);
            }
        }

        public static Neuron GetNeuronAt(int y, int x, CnnNet network)
        {
            return network.NeuronPositionMap[y, x];
        }

        public static Neuron[] GetNeuronsWithinRange(int posX, int posY, CnnNet network, int range)
        {
            var result = new List<Neuron>();

            int minX = Math.Max(posX - range, 0);
            int maxX = Math.Min(posX + range, network.Width - 1);

            int minY = Math.Max(posY - range, 0);
            int maxY = Math.Min(posY + range, network.Height - 1);

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    if ((x == posX && y == posY)
                        || GetDistance(posX, posY, x, y) > range)
                    {
                        continue;
                    }

                    var neuron = GetNeuronAt(y, x, network);

                    if (neuron != null)
                    {
                        result.Add(neuron);
                    }
                }
            }

            return result.ToArray();
        }

        public static Neuron GetClosestNeuronsWithinRange(int posX, int posY, CnnNet network, int range)
        {
            Neuron result = null;

            int minX = Math.Max(posX - range, 0);
            int maxX = Math.Min(posX + range, network.Width - 1);

            int minY = Math.Max(posY - range, 0);
            int maxY = Math.Min(posY + range, network.Height - 1);

            double minDistance = double.MaxValue;

            for (int y = minY; y < maxY; y++)
            {
                for (int x = minX; x < maxX; x++)
                {
                    double distance;
                    if ((distance = GetDistance(posX, posY, x, y)) > range
                        || distance > minDistance)
                    {
                        continue;
                    }

                    var neuron = GetNeuronAt(y, x, network);
                    if (neuron != null)
                    {
                        minDistance = distance;
                        result = neuron;
                    }
                    
                }
            }

            return result;
        }

        public static Neuron[] GetNeuronsWithAxonTerminalWithinRange(int posX, int posY, CnnNet network, int range)
        {
            int minX = Math.Max(posX - range, 0);
            int maxX = Math.Min(posX + range, network.Width - 1);

            int minY = Math.Max(posY - range, 0);
            int maxY = Math.Min(posY + range, network.Height - 1);

            return network.Neurons.Where
                (neuron => neuron.PosX != posX
                           && neuron.PosY != posY
                           && neuron.HasAxonReachedFinalPosition
                           && minX <= neuron.AxonTerminal.X && neuron.AxonTerminal.X <= maxX
                           && minY <= neuron.AxonTerminal.Y && neuron.AxonTerminal.Y <= maxY
                           && GetDistance(neuron.AxonTerminal.X, neuron.AxonTerminal.Y, posX, posY) <= range).ToArray();
        }

        public static void GetMaxAndLocation(this double[,] map, out IEnumerable<Point> locations, out double maxValue)
        {
            var locationsList = new List<Point>();
            maxValue = 0;

            for (int y = 0; y < map.GetLength(0); y++)
            {
                for (int x = 0; x < map.GetLength(1); x++)
                {
                    if (map[y, x] == maxValue)
                    {
                        locationsList.Add(new Point(x, y));
                    }
                    else if (map[y, x] > maxValue)
                    {
                        locationsList.Clear();
                        locationsList.Add(new Point(x, y));
                        maxValue = map[y, x];
                    }
                }
            }

            locations = locationsList.ToArray();
        }

        public static double[,] Sum(this IEnumerable<double[,]> maps)
        {
            Contract.Requires<ArgumentNullException>(maps != null);
            Contract.Requires<ArgumentException>(maps.Any(), "maps must contains at least one item");

            var mapsList = maps.ToList();

            var result = (double[,])mapsList.ElementAt(0).Clone();
            foreach (var map in mapsList.Skip(1))
            {
                for (int y = 0; y < map.GetLength(0); y++)
                {
                    for (int x = 0; x < map.GetLength(1); x++)
                    {
                        result[y, x] += map[y, x];
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Remember to optimize this by using spiral matrix processing
        /// http://pastebin.com/4EYJvv5X
        /// </summary>
        /// <param name="referenceY"></param>
        /// <param name="referenceX"></param>
        /// <param name="neuron"></param>
        /// <returns></returns>
        public static double GetDistanceToNearestNeuron(int referenceY, int referenceX, Neuron neuron, CnnNet network)
        {
            double minDistance = network.MinDistanceBetweenNeurons + 1;

            int xMin = Math.Max(referenceX - network.MinDistanceBetweenNeurons, 0);
            int xMax = Math.Min(referenceX + network.MinDistanceBetweenNeurons, network.Width - 1);
            int yMin = Math.Max(referenceY - network.MinDistanceBetweenNeurons, 0);
            int yMax = Math.Min(referenceY + network.MinDistanceBetweenNeurons, network.Height - 1);

            for (int y = yMin; y <= yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    if (network.NeuronPositionMap[y, x] != null
                        && network.NeuronPositionMap[y, x] != neuron)
                    {
                        var distance = Extensions.GetDistance(referenceX, referenceY, x, y);
                        if (minDistance > distance)
                        {
                            minDistance = distance;
                        }
                    }
                }
            }

            return minDistance;
        }
    }
}