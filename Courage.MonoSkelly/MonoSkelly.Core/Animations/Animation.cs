/**
 * MonoSkelly Animation.
 * Ronen Ness 2021
 */
using System;
using System.Collections.Generic;
using System.Linq;


namespace MonoSkelly.Core
{
    /// <summary>
    /// Represent an animation attached to a skeleton.
    /// </summary>
    public class Animation
    {
        // animation steps
        List<AnimationStep> _steps = new List<AnimationStep>();

        /// <summary>
        /// Get animation steps count.
        /// </summary>
        public int StepsCount => _steps.Count;

        /// <summary>
        /// If true, animation will interpolate from last step back to first step.
        /// If false, will just freeze on last step.
        /// </summary>
        public bool Repeats;

        /// <summary>
        /// Get animation steps.
        /// </summary>
        public IReadOnlyList<AnimationStep> Steps => _steps.AsReadOnly();

        // skeleton this animation is attached to.
        internal Skeleton _skeleton;

        /// <summary>
        /// Animation name.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Create the animation instance.
        /// </summary>
        /// <param name="skeleton">Base skeleton.</param>
        /// <param name="name">Animation name.</param>
        internal Animation(Skeleton skeleton, string name)
        {
            _skeleton = skeleton;
            Name = name;
        }

        /// <summary>
        /// Clone this animation.
        /// </summary>
        public Animation Clone()
        {
            var clone = new Animation(_skeleton, Name);
            foreach (var step in _steps)
            {
                clone.AddStep(step.Clone(this));
            }
            clone.Repeats = Repeats;
            return clone;
        }

        /// <summary>
        /// Remove an animation step by index.
        /// </summary>
        public void RemoveStep(int index)
        {
            _steps.RemoveAt(index);
        }

        /// <summary>
        /// Rename a bone.
        /// </summary>
        public void RenameBone(string fromPath, string toPath)
        {
            // rename animation bones
            foreach (var step in _steps)
            {
                step.RenameBone(fromPath, toPath);
            }
        }

        /// <summary>
        /// Add animation step.
        /// </summary>
        /// <param name="stepName">Optional step name.</param>
        /// <param name="duration">Step duration.</param>
        /// <param name="copyTransformFrom">If set, will copy all transformations from this step.</param>
        public void AddStep(string stepName = "", float duration = 1f, AnimationStep copyTransformFrom = null)
        {
            // create step
            var step = (copyTransformFrom != null) ? 
                copyTransformFrom.Clone(this) : 
                new AnimationStep(this);
            step.Name = stepName;
            step.Duration = duration;

            // set all bones default transforms
            if (copyTransformFrom == null)
            {
                foreach (var transform in _skeleton.GetAllDefaultTransformations())
                {
                    step.SetTransform(transform.Key, transform.Value);
                }
            }

            // add step
            AddStep(step);
        }

        /// <summary>
        /// Add an animation step.
        /// </summary>
        /// <param name="step">Animation step to add.</param>
        public void AddStep(AnimationStep step)
        {
            _steps.Add(step);
        }

        /// <summary>
        /// Set an animation step value.
        /// </summary>
        /// <param name="index">Index to set.</param>
        /// <param name="step">New animation step value.</param>
        public void SetStep(int index, AnimationStep step)
        {
            _steps[index] = step;
        }

        /// <summary>
        /// Split a given step based on time offset.
        /// </summary>
        public void Split(float time)
        {
            // iterate steps to find which one to split
            var currOffset = 0f;
            var index = 0;
            foreach (var step in _steps.ToArray())
            {
                // hit the begining of step, nothing to do
                if (time == currOffset) { return; }

                // did we find the step to break?
                if ((time > currOffset) && (time < currOffset + step.Duration))
                {  
                    var originDuration = step.Duration;
                    step.Duration = time - currOffset;
                    var newStep = step.Clone();
                    newStep.Duration = originDuration - step.Duration;
                    _steps.Insert(index, newStep);   
                    return;
                }

                // advance offset and index
                currOffset += step.Duration;
                index++;
            }
        }

        /// <summary>
        /// Get animation step from index.
        /// </summary>
        /// <param name="wrapIfOutOfIndex">If true, will wrap index if out of range.</param>
        public AnimationStep GetStep(int index, bool wrapIfOutOfIndex = false)
        {
            if (index >= _steps.Count)
            {
                if (wrapIfOutOfIndex)
                {
                    index = index % _steps.Count;
                }
                else
                {
                    index = _steps.Count - 1;
                }
            }
            return _steps[index];
        }

		/// <summary>
		/// Get animation step from index.
		/// </summary>
		/// <param name="step">If true, will wrap index if out of range.</param>
		public int GetStepIndex(AnimationStep step)
		{
            return _steps.IndexOf(step);
		}

        // TODO: This method is incorrect; fix it
		public void SetStepAtTimestamp(AnimationStep step, float timestamp)
		{
            {
                // Calculate the total duration of the animation
                float totalDuration = _steps.Sum(x => x.Duration);
                float totalDurationMinusStep = totalDuration - step.Duration;

                // If the timestamp is greater than the total duration, adjust the step duration and add it at the end
                if(timestamp >= totalDurationMinusStep)
                {
                    step.Duration = timestamp - totalDurationMinusStep;
                    _steps.Remove(step);
                    _steps.Add(step);
                    return;
                }
            }

			// Find the correct position for the step
			float startTime = 0f;
			int index = 0;
			for(; index < _steps.Count; index++)
			{
				if(startTime + _steps[index].Duration > timestamp)
				{
					break;
				}
				startTime += _steps[index].Duration;
			}

            int previousIndex = _steps.IndexOf(step);

            // Insert the step at the correct index
            if(index != previousIndex)
            {
                _steps.Insert(index, step);
                if(previousIndex >= 0)
                {
                    // If previous index is before new position, then we need to remove at the same position
                    // since it has no impact on it. Otherwise the previous index has changed
                    _steps.RemoveAt(index > previousIndex ?
                        previousIndex :
                        previousIndex + 1
                        );
                }
            }

			// Adjust the duration of the step
			step.Duration = timestamp - startTime;

            // TODO: The problem is here, subsequent durations should be calculated as
            // The portion of the old subsequent duration discarding the portion of the moved duration which overlaps
            // I am not sure how to compute this for now
            float currentTime = timestamp;

			// Adjust the durations of the subsequent steps
			for(int i = index + 1; i < _steps.Count; i++)
			{
				// Update the start time of the subsequent steps
				currentTime += _steps[i].Duration;
				
				// Update the duration of the step
				_steps[i].Duration = currentTime - currentTime;
				
			}
		}
	}
}
