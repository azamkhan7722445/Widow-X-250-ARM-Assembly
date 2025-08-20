using Fusion;
using Fusion.Addons.SubscriberRegistry;

namespace Fusion.Addons.Learning
{
    /// <summary>
    /// This class is used as a parent class for Learner and LearnerActivityTracker classes.
    /// In this way, they benefit from the Subscriber class registration mechanism defined in the SubscriberRegistry addon.
    /// </summary>
    public abstract class LearnerComponent : Subscriber<LearnerComponent>
    {
        public abstract NetworkString<_64> UserId { get; }
        public override bool UnregisterWhenNotAvailable => true;

        public override bool IsAvailable => base.IsAvailable && UserId != "";
    }
}
