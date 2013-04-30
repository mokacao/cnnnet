﻿using System;
using System.Linq;
using System.Collections.Generic;

namespace CnnNetLib2
{
    public class Neuron
    {
        #region Fields

        public int Id;
        private readonly CnnNet _cnnNet;

        public int PosX;
        public int PosY;
        public bool HasReachedFinalPosition;

        private double _movedDistance;
        private int _neuronIterationsLeftBeforeFinalPosition;
        private int _iterationsSinceLastActivation;

        #endregion

        #region Properties

        public bool IsActive
        {
            get
            {
                return _cnnNet.ActiveNeurons.Any(neuron => neuron == this);
            }
        }

        #endregion

        #region Methods

        public void Process()
        {
            if (HasReachedFinalPosition)
            {
                #region Increase region desirability if neuron fires

                if (IsActive)
                {
                    AddDesirability();
                    _iterationsSinceLastActivation = 0;
                }

                #endregion

                #region Else increase region UN-desirability

                else
                {
                    _iterationsSinceLastActivation++;
                    AddUndesirability();
                }

                #endregion
            }
            else
            {
                #region Neuron searches for better position

                if ((_cnnNet.InputNeuronsMoveToHigherDesirability
                     || _cnnNet.InputNeurons.All(inpNeuron => inpNeuron != this))
                    &&
                    _movedDistance < _cnnNet.MaxNeuronMoveDistance)
                {
                    _neuronIterationsLeftBeforeFinalPosition =
                        ProcessMoveToHigherDesirability()
                        ? _cnnNet.NeuronIterationCountBeforeFinalPosition
                        : _neuronIterationsLeftBeforeFinalPosition - 1;

                    if (_neuronIterationsLeftBeforeFinalPosition == 0)
                    {
                        HasReachedFinalPosition = true;
                    }
                }

                #endregion
            }
        }

        private void AddDesirability()
        {
            AddProportionalRangedValue
                (_cnnNet.NeuronDesirabilityMap, _cnnNet.Width, _cnnNet.Height,
                _cnnNet.NeuronDesirabilityInfluenceRange, 
                _cnnNet.NeuronDesirabilityMaxInfluence);
        }

        private void AddUndesirability()
        {
            AddProportionalRangedValue
                (_cnnNet.NeuronUndesirabilityMap, _cnnNet.Width, _cnnNet.Height,
                _cnnNet.NeuronUndesirabilityInfluenceRange,
                _cnnNet.NeuronUndesirabilityMaxInfluence * Math.Max(1, _iterationsSinceLastActivation / _cnnNet.NeuronUndesirabilityMaxIterationsSinceLastActivation));
        }

        private void AddProportionalRangedValue(double[,] map, int width, int height, int influencedRange, double maxValue)
        {
            int xMin = Math.Max(PosX - influencedRange, 0);
            int xMax = Math.Min(PosX + influencedRange, width);
            int yMin = Math.Max(PosY - influencedRange, 0);
            int yMax = Math.Min(PosY + influencedRange, height);

            for (int y = yMin; y < yMax; y++)
            {
                for (int x = xMin; x < xMax; x++)
                {
                    var distance = Extensions.GetDistance(PosX, PosY, x, y);

                    var influenceByRange = Math.Max(influencedRange - distance, 0);

                    map[y, x] = Math.Min(1, map[y, x] + influenceByRange / influencedRange * maxValue);
                }
            }
        }

        private bool ProcessMoveToHigherDesirability()
        {
            bool ret = false;

            int minCoordX = Math.Max(PosX - _cnnNet.NeuronDesirabilityInfluenceRange, 0);
            int maxCoordX = Math.Min(PosX + _cnnNet.NeuronDesirabilityInfluenceRange, _cnnNet.Width - 1);

            int minCoordY = Math.Max(PosY - _cnnNet.NeuronDesirabilityInfluenceRange, 0);
            int maxCoordY = Math.Min(PosY + _cnnNet.NeuronDesirabilityInfluenceRange, _cnnNet.Height - 1);

            int maxDesirabX = PosX;
            int maxDesirabY = PosY;
            double maxDesirabMovedDistance = 0;
            double maxDesirability = _cnnNet.NeuronDesirabilityMap[PosY, PosX];

            for (int y = minCoordY; y < maxCoordY; y++)
            {
                for (int x = minCoordX; x < maxCoordX; x++)
                {
                    if (x == PosX && y == PosY)
                    {
                        continue;
                    }

                    if (_cnnNet.NeuronDesirabilityMap[y, x] > maxDesirability
                        && GetNeuronAt(y, x) == null
                        && _movedDistance + Extensions.GetDistance(PosX, PosY, x, y) < _cnnNet.MaxNeuronMoveDistance
                        && GetDistanceToNearestNeuron(y, x) >= _cnnNet.MinDistanceBetweenNeurons
                        && Extensions.GetDistance(PosX, PosY, x, y) <= _cnnNet.NeuronDesirabilityInfluenceRange /* this ensures that we only check within the range */)
                    {
                        maxDesirabX = x;
                        maxDesirabY = y;
                        maxDesirability = _cnnNet.NeuronDesirabilityMap[y, x];
                        maxDesirabMovedDistance = Extensions.GetDistance(PosX, PosY, x, y);
                    }
                }
            }

            if (PosX != maxDesirabX
                && PosY != maxDesirabY)
            {
                MoveTo(maxDesirabY, maxDesirabX);
                _movedDistance += maxDesirabMovedDistance;

                ret = true;
            }

            return ret;
        }

        private void MoveTo(int newPosY, int newPosX)
        {
            PosX = newPosX;
            PosY = newPosY;
        }

        private double GetDistanceToNearestNeuron(int referenceY, int referenceX)
        {
            double distanceToNearestNeuron = _cnnNet.NeuronDesirabilityInfluenceRange + 1;

            int xMin = Math.Max(referenceX - _cnnNet.MinDistanceBetweenNeurons, 0);
            int xMax = Math.Min(referenceX + _cnnNet.MinDistanceBetweenNeurons, _cnnNet.Width - 1);
            int yMin = Math.Max(referenceY - _cnnNet.MinDistanceBetweenNeurons, 0);
            int yMax = Math.Min(referenceY + _cnnNet.MinDistanceBetweenNeurons, _cnnNet.Height - 1);

            var distances = _cnnNet.Neurons.Where(neuron =>
                xMin <= neuron.PosX
                && neuron.PosX <= xMax
                && yMin <= neuron.PosY
                && neuron.PosY <= yMax).
                Select(neuron => Extensions.GetDistance(referenceX, referenceY, neuron.PosX, neuron.PosY)).ToArray();

            return distances.Any()
                       ? distances.Min()
                       : distanceToNearestNeuron;
        }

        private Neuron GetNeuronAt(int y, int x)
        {
            return _cnnNet.Neurons.FirstOrDefault(neuron => neuron.PosX == x && neuron.PosY == y);
        }

        private Neuron[] GetNeuronsWithinRange(int range)
        {
            var ret = new List<Neuron>();

            int minCoordX = Math.Max(PosX - range, 0);
            int maxCoordX = Math.Min(PosX + range, _cnnNet.Width - 1);

            int minCoordY = Math.Max(PosY - range, 0);
            int maxCoordY = Math.Min(PosY + range, _cnnNet.Height - 1);

            for (int y = minCoordY; y < maxCoordY; y++)
            {
                for (int x = minCoordX; x < maxCoordX; x++)
                {
                    if ((x == PosX && y == PosY)
                        || Extensions.GetDistance(PosX, PosY, x, y) > range)
                    {
                        continue;
                    }

                    var neuron = GetNeuronAt(y, x);

                    if (neuron != null)
                    {
                        ret.Add(neuron);
                    }
                }
            }

            return ret.ToArray();
        }

        #endregion

        #region Instance

        public Neuron(int id, CnnNet cnnNet)
        {
            Id = id;
            _cnnNet = cnnNet;
            _neuronIterationsLeftBeforeFinalPosition = _cnnNet.NeuronIterationCountBeforeFinalPosition;
        }

        #endregion
    }
}
