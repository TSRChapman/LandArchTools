using System;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input.Custom;
using Rhino.Input;
using LandArchTools.Utilities;

namespace LandArchTools.Commands
{
    public class MoveToRLCommand : Command
    {
        public MoveToRLCommand()
        {
            Instance = this;
        }

        ///<summary>The only instance of the MyCommand command.</summary>
        public static MoveToRLCommand Instance { get; private set; }

        public override string EnglishName => "MoveToRL";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                (double scale, bool imperial) = scaleHelper.Scaling(doc);

                var go = new GetObject();
                go.SetCommandPrompt("Select objects");
                go.GroupSelect = true;
                go.SubObjectSelect = false;
                go.GetMultiple(1, 0);
                if (go.CommandResult() != Result.Success)
                    return go.CommandResult();

                var gp = new GetPoint();
                gp.SetCommandPrompt("Select point");
                gp.Get();
                if (gp.CommandResult() != Result.Success)
                    return gp.CommandResult();

                var currentPoint = gp.Point();
                var rlStr = imperial ? "RL (ft) to move to?" : "RL (m) to move to?";
                double rl = 0;
                var res = RhinoGet.GetNumber(rlStr, true, ref rl);
                if (res != Result.Success)
                    return res;

                // Convert to meters or feet
                rl /= scale;
                

                Vector3d moveVector = Vector3d.Zero;

                if (rl == 0)
                {
                    moveVector = new Vector3d(0, 0, -currentPoint.Z);
                }
                else
                {
                    moveVector = new Vector3d(0, 0, rl - currentPoint.Z);
                }

                foreach (var objRef in go.Objects())
                {
                    var obj = objRef.Object();
                    if (obj != null)
                    {
                        obj.Geometry.Translate(moveVector);
                        obj.CommitChanges();
                    }
                }

                doc.Views.Redraw();
                return Result.Success;
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine($"Failed to execute: {ex.Message}");
                return Result.Failure;
            }
        }

    }
}