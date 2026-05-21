namespace CareAdminTestProject.Incidents.IncidentDetails.Tests
{
    /// <summary>
    /// Encapsulates functional workflow and lifecycle integration tests for incident records.
    /// Covers full document generation, sequential multi-role digital signature sign-offs, 
    /// state locking/unlocking capabilities, and post-approval completeness indicator verifications.
    /// </summary>
    [TestFixture]
    internal class IncidentWorkflowTests : BaseIncidentTests
    {
        /// <summary>
        /// Validates the standard happy path workflow lifecycle: populates all data tabs, 
        /// applies progressive signatures for DNS, MD, and Administrator roles, and verifies automated record locking.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignIncident()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Validates an alternative orchestration path for the lifecycle signing sequence, 
        /// ensuring system state persistence remains consistent across uniform multi-role sign-offs.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignIncidentOtherWay()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.SignMD();
            await steps.SignAdministrator();
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Validates advanced state toggle capabilities: verifies the workspace automatically locks post signature, 
        /// successfully switches to unlocked status via security controls, and locks down again upon re-activation.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateAndSignUnlockAndLockIncident()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();
            await steps.SetIncidentLockStateAsync(false);
            await steps.AssertIncidentIsUnlockedAsync();
            await steps.SetIncidentLockStateAsync(true);
            await steps.AssertIncidentIsLockedAsync();
        }

        /// <summary>
        /// Comprehensive status integration test: verifies that once a document finishes full creation 
        /// and transitions into a locked status state, every tab header completely hides its incomplete indicator badge.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        [Test]
        public async Task CreateSignAndVerifyCompleteness()
        {
            await steps.FillAndSaveEntireIncident(data);
            await steps.SignDNS();
            await steps.AssertIncidentIsLockedAsync();
            await steps.VerifyRedDotTab("General", false);
            await steps.VerifyRedDotTab("Details", false);
            await steps.VerifyRedDotTab("State", false);
            await steps.VerifyRedDotTab("Medication", false);
            await steps.VerifyRedDotTab("Summary", false);
            await steps.VerifyRedDotTab("Attachments", false);
        }
    }
}
