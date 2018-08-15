//
//  THIS CODE AND INFORMATION IS PROVIDED "AS IS" WITHOUT WARRANTY OF ANY
//  KIND, EITHER EXPRESSED OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE
//  IMPLIED WARRANTIES OF MERCHANTABILITY AND/OR FITNESS FOR A PARTICULAR
//  PURPOSE. IT CAN BE DISTRIBUTED FREE OF CHARGE AS LONG AS THIS HEADER 
//  REMAINS UNCHANGED.
//
//  Email:  gustavo_franco@hotmail.com
//
//  Copyright (C) 2006 Franco, Gustavo 
//

using Microsoft.Xna.Framework;
using Ship_Game;

namespace Algorithms
{
    [Author("Franco, Gustavo")]
    internal interface IPathFinder
    {
        #region Events
        event PathFinderDebugHandler PathFinderDebug;
        #endregion

        #region Properties
        bool Stopped
        {
            get;
        }

        HeuristicFormula Formula
        {
            get;
            set;
        }

        bool Diagonals
        {
            get;
            set;
        }

        bool HeavyDiagonals
        {
            get;
            set;
        }

        int HeuristicEstimate
        {
            get;
            set;
        }

        bool PunishChangeDirection
        {
            get;
            set;
        }

        bool ReopenCloseNodes
        {
            get;
            set;
        }

        bool TieBreaker
        {
            get;
            set;
        }

        int SearchLimit
        {
            get;
            set;
        }

        double CompletedTime
        {
            get;
            set;
        }

        bool DebugProgress
        {
            get;
            set;
        }

        bool DebugFoundPath
        {
            get;
            set;
        }
        #endregion

        #region Methods
        void FindPathStop();
        Array<PathFinderNode> FindPath(Point start, Point end);
        #endregion

    }
}
