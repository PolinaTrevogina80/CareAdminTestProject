using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;
using System.Xml;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab.RNSupervisorTabInfo;
using static GeneralTab;
using static IncidentCreatePage;

/// <summary>
/// Service factory class utilizing Object Mother and Static Factory patterns to construct consolidated test datasets.
/// Generates comprehensive incident records tailored to specified timezones and profile parameters.
/// </summary>
public static class IncidentDataFactory
{
    /// <summary>
    /// Encapsulates the entire multi-tab dataset required to fully populate and verify an incident report.
    /// </summary>
    public record IncidentTestData(
        IncidentGeneralInfo General,
        DetailsTab.IncidentDetailsInfo Details,
        StateTab.IncidentStateInfo State,
        List<MedicationTab.MedicationInfo> Medications,
        RNSupervisorTabInfo RNSupervisor,
        SummaryTab.IncidentSummaryInfo Summary
        );

    /// <summary>
    /// Creates a typical baseline dataset representing a standard Fall incident (Object Mother pattern).
    /// </summary>
    /// <param name="residentInfo">Biographical and room/bed placement attributes extracted for the target profile.</param>
    /// <returns>A fully structured <see cref="IncidentTestData"/> model initialized with Pacific Standard Time metrics.</returns>
    public static IncidentTestData CreateDefaultFall(ResidentInfo residentInfo)
    {
        var usTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Pacific Standard Time");
        var usNow = TimeZoneInfo.ConvertTime(DateTime.Now, usTimeZone);

        return new IncidentTestData(
            General: CreateDefaultGeneral(residentInfo),
            Details: CreateDefaultDetails(usNow),
            State: CreateDefaultState(),
            Medications: CreateDefaultMedications(),
            RNSupervisor: CreateDefaultRNSupervisor(),
            Summary: CreateDefaultSummary()
        );
    }


    /// <summary>
    /// Creates a customized variation of the baseline dataset representing a Medication Error incident.
    /// </summary>
    /// <param name="residentInfo">Biographical and room/bed placement attributes extracted for the target profile.</param>
    /// <returns>An adjusted <see cref="IncidentTestData"/> instance matching medication deviation parameters.</returns>
    public static IncidentTestData CreateMedicationError(ResidentInfo residentInfo)
    {
        var baseData = CreateDefaultFall(residentInfo);
        return baseData with
        {
            General = baseData.General with { type = "Medication Error", summary = "Wrong dosage" }
        };
    }

    // --- Private builder methods (Static Factory pattern) ---

    /// <summary>
    /// Constructs the initial dataset for the General tab form fields, aligning runtime system dates with Eastern Standard Time.
    /// </summary>
    /// <param name="residentInfo">Biographical and room/bed placement attributes extracted for the target profile.</param>
    /// <returns>An initialized <see cref="IncidentGeneralInfo"/> data record model.</returns>
    private static IncidentGeneralInfo CreateDefaultGeneral(ResidentInfo residentInfo)
    {
        // 1. Determine the Eastern Time zone (New York/EDT)
        var edtZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        // 2. Convert PC's current time to EDT time
        DateTime edtNow = TimeZoneInfo.ConvertTime(DateTime.Now, edtZone);

        // 3. Calculate how many minutes have passed since the beginning of today (00:00) in the EDT time zone
        int minutesPassedInDay = (int)(edtNow - edtNow.Date).TotalMinutes;

        // 4. Generate a unique shift STRICTLY BACK(in the past)
        // At least 2 minutes back (to bypass micro-lags of server desynchronization), 
        // but no more than has passed since the beginning of the day, so as not to fly into yesterday.
        // Limit the step to a maximum of 60 minutes to stay within the current day.
        var random = new Random();
        int minShift = Math.Min(2, minutesPassedInDay);
        int maxShift = Math.Max(2, Math.Min(60, minutesPassedInDay));

        int randomMinuteShift = random.Next(minShift, maxShift);

        // 5. Subtract the shift from the current time. The time is guaranteed to be unique, in the past, and within the current date!
        var uniqueTime = new TimeOnly(edtNow.Hour, edtNow.Minute).Add(TimeSpan.FromMinutes(-randomMinuteShift));


        return new IncidentGeneralInfo(
            room: residentInfo.Room,
            bed: residentInfo.Bed,
            date: edtNow.Date, 
            time: uniqueTime,
            unit: "2",
            location: "Lobby",
            type: "Fall",
            supervisor: 1,
            chargeNurse: 1,
            cna: 1,
            activity: "Self-Transferring",
            summary: "Patient found on the floor",
            injury: new List<InjuryInfo>
            {
            new InjuryInfo("Hematoma", "Left Knee", "2", "3", "1")
            }
        );
    }

    /// <summary>
    /// Constructs the dataset for the Details tab form fields, mapping medical assessment details and notification time logs.
    /// </summary>
    /// <param name="now">The system timestamp used as a base reference configuration.</param>
    /// <returns>An initialized <see cref="DetailsTab.IncidentDetailsInfo"/> data record model.</returns>
    private static DetailsTab.IncidentDetailsInfo CreateDefaultDetails(DateTime now)
    {
        var edtZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");
        DateTime edtNow = TimeZoneInfo.ConvertTime(DateTime.Now, edtZone);
        TimeOnly edtTime = new TimeOnly(edtNow.Hour, edtNow.Minute);
        return new(
            OccurrenceDescription: "Patient fell while walking",
            PatientDescription: "I slipped on the wet floor",
            FirstAidAdministered: true,
            FirstAidDescribe: "Ice pack applied to left knee",
            VitalSigns: new DetailsTab.VitalSigns("98.6", "80", "120/80", "98", "118/75", "110"),
            ResidentTransferred: false,
            CorrectiveAction: "Floor was dried immediately",
            PreventiveAction: "Increase monitoring during cleaning",
            RelativeNotified: new DetailsTab.RelativeNotification("John Doe", "Son", "Nurse Smith", edtNow.Date, edtTime),
            MDNotified: new DetailsTab.MDNotification("Dr. House", "Nurse Smith", edtNow.Date, edtTime),
            MDOrder: "X-ray of the left knee",
            DiagnosticTests: "None",
            Witnesses: new List<string> { "Witness One", "Witness Two" }
    );
    }

    /// <summary>
    /// Constructs the dataset for the State tab checkboxes, radio option metrics, and assistive devices tracking.
    /// </summary>
    /// <returns>An initialized <see cref="StateTab.IncidentStateInfo"/> data record model.</returns>
    private static StateTab.IncidentStateInfo CreateDefaultState() => new(
            Communication: new StateTab.CommunicationStatus(
                Oriented: true, Person: true, Time: true, Place: true,
                Alert: true,
                Confused: false,
                Forgetful: false,
                Uncooperative: false,
                NonCompliant: false,
                Agitated: false,
                NonVerbal: false,
                BlindDeaf: false,
                LanguageBarrier: false
                ),
            AmbulatoryStatus: "Non Ambulatory", // Matches the text display property on the radio button
            NeedsAssistanceOf: "One staff member",
            UsesLift: false,
            NeedsSupervision: true,
            Restrained: false,
            TypeOfRestraint: "None",
            SideRail: true,
            NotInvolved: false,
            BowelBladder: new StateTab.BowelBladderStatus(
                Foley: false,
                Colostomy: false,
                Continent: true,
                Incontinent: false,
                Bowel: true,
                Bladder: true
                ),
            Alarms: new StateTab.AlarmsStatus(
                NoAlarmOrder: false,
                BedAlarm: true,
                ChairAlarm: false,
                PinAlarm: false,
                OtherType: false
                ),
            OtherAlarmDetails: "",
            Devices: new StateTab.AssistiveDevices(
                Wheelchair: "Not Used",
                WalkerCrutch: "Used",
                Cane: "Not Used",
                HearingAid: "Not Used",
                Glasses: "Used"
                )
    );

    /// <summary>
    /// Constructs a basic list array collection containing sample medication entry rows.
    /// </summary>
    /// <returns>A initialized collection list containing <see cref="MedicationTab.MedicationInfo"/> models.</returns>
    private static List<MedicationTab.MedicationInfo> CreateDefaultMedications() => new() {
        new ("Aspirin", "100mg", "Once a day", "08:00"),
        new ("Nurafen", "250mg", "Twice a day", "15:00"),
        new ("Melatonin", "5mg", "Before sleep", "21:00")
    };

    /// <summary>
    /// Constructs the dataset for the RN/Supervisor questionnaire tab, populating location selection arrays, 
    /// last-seen metadata timelines, and 25 sequential validation step query parameters.
    /// </summary>
    /// <returns>An initialized <see cref="RNSupervisorTab.RNSupervisorTabInfo"/> data record model.</returns>
    private static RNSupervisorTab.RNSupervisorTabInfo CreateDefaultRNSupervisor() => new(
        Locations: new[] { "ADL Suite", "Dining Room" },
        LastSeen: new RNSupervisorTabInfo.LastSeenInfo(
            Time: new TimeOnly(09, 00),
            Details: "1. The resident was resting in the armchair, watching TV."
        ),
        DescribeExactly: new RNSupervisorTabInfo.DescribeExactlyInfo(
            Details: "2. The resident stood up and fall down."
            ),
    Questions: new List<QuestionWithDetails>
    {
        /* 4  */ new(true, "3. He always fall down after talkshows"), // Was anyone with the resident?
        /* 4  */ new(true, "4. CNA Maria was emptying the trash nearby"), // Was anyone with the resident?
        /* 5  */ new(true, "5. Call bell was on the bedside table"),       // Call bell within reach?
        /* 6  */ new(true, "6. Glasses and water were on the tray"),      // Personal items within reach?
        /* 7  */ new(false, "7. Resident often forgets to wait for assistance"), // Compliant with instructions?
        /* 8  */ new(true, "8. Cognitively alert but physically impulsive"), // Cognitively able to call?
        /* 9  */ new(false, "9. Call light was not activated"),           // Call light on?
        /* 10 */ new(true, "10. Thought it was 1974 and he was at a disco"), // Was confused?
        /* 11 */ new(false, "11. No restraints were in use"),              // Was restrained?
        /* 12 */ new(false, "12. No changes in restraint protocol"),       // Change in restraint last month?
        /* 13 */ new(true, "13. Tried to scale the side rails like a pro"), // Climb over side rails?
        /* 14 */ new(true, "14. Bed was in the lowest position"),          // Bed in low position?
        /* 15 */ new(false, "15. Vision is adequate with glasses"),        // Visually impaired?
        /* 16 */ new(false, "16. Floor was dry as a desert, no clutter"),  // Physical hazards?
        /* 17 */ new(true, "17. Was wearing only the left slipper"),       // Wearing shoes?
        /* 18 */ new(true, "18. Rolling walker found 2 meters away"),      // Using assistive device?
        /* 19 */ new(true, "19. Known history of occasional incontinence"), // Is incontinent?
        /* 20 */ new(true, "20. Urgent need to reach the bathroom"),       // Trying to ambulate for toileting?
        /* 21 */ new(false, "21. Last toileted 3 hours prior"),            // Toileted within past 2 hours?
        /* 22 */ new(true, "22. Alarm sounded but resident was too fast"), // Electronic monitor used?
        /* 23 */ new(true, "23. Complained of slight dizziness and 'wobbly knees'"), // Physical complaints?
        /* 24 */ new(true, "24. Increased leaning to the left noticed yesterday"), // Changes in mobility/balance?
        /* 25 */ new(true, "25. Currently on Warfarin"),                   // On blood thinner?
        /* 26 */ new(true, "26. Paper-thin skin on forearms, minor bruising"), // Fragile skin?
        /* 27 */ new(false, "27. Medication regime remains stable"),       // Medication changes?
        /* 28 */ new(true, "28. Added 'bed alarm' and 'frequent checks'")  // Care plan updated?
    }
);

    /// <summary>
    /// Constructs the final dataset for the Summary tab, aggregating legal status approvals, multidisciplinary conclusions, and target signature descriptors.
    /// </summary>
    /// <returns>An initialized <see cref="SummaryTab.IncidentSummaryInfo"/> data record model.</returns>
    private static SummaryTab.IncidentSummaryInfo CreateDefaultSummary() => new(
    CarePlanUpdated: true,
    SetAsReportable: true,
    MajorInjury: true,
    SendToLegal: true,
    Summary: "The incident was reviewed by the interdisciplinary team. Patient's condition is stable.",
    Plan: "Continue monitoring vital signs every 4 hours for the next 24 hours and update care plan goals.",
    Conclusion: "Unavoidable", // This text value configuration must exactly replicate the UI radio button label string text
    EvidenceOfAbuse: false,
    EvidenceReason: " The resident was observed sitting on the floor and the resident was not able to give " +
        "details of what occurred secondary to confusion.  It was concluded from the review of the camera footage " +
        "and staff statements that a fall had occurred in the absence of evidence suggesting otherwise. There were " +
        "no elements suggesting that abuse, neglect, or mistreatment occurred ",
    ReportedToAgency: false,
    PossibleContributingFactor: new[] { "FALLS WITHIN 30 DAYS ADMISSION", "UNDERLYING CHRONIC CONDITION" }, // Resolves to an active matching option value within your Dropdown select lists
    DirectorSignature: "Polly Test" // The target user profile name inserted inside the formal signature field wrapper container
);
}
