using static GeneralTab;
using static IncidentCreatePage;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab;
using static CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs.RNSupervisorTab.RNSupervisorTabInfo;
using CareAdminTestProject.Incidents.IncidentDetails.Pages.IncidentTabs;

public static class IncidentDataFactory
{
    public record IncidentTestData(
        IncidentGeneralInfo General,
        DetailsTab.IncidentDetailsInfo Details,
        StateTab.IncidentStateInfo State,
        List<MedicationTab.MedicationInfo> Medications,
        RNSupervisorTab.RNSupervisorTabInfo RNSupervisor,
        SummaryTab.IncidentSummaryInfo Summary
        );
    // Базовый метод для создания типичного инцидента (Object Mother)
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


    // Сценарий для другого типа инцидента (Object Mother)
    public static IncidentTestData CreateMedicationError(ResidentInfo residentInfo)
    {
        var baseData = CreateDefaultFall(residentInfo);
        return baseData with
        {
            General = baseData.General with { type = "Medication Error", summary = "Wrong dosage" }
        };
    }

    // --- Приватные методы для сборки частей (Static Factory) ---

    private static IncidentGeneralInfo CreateDefaultGeneral(ResidentInfo residentInfo)
    {
        // 1. Опеределяем часовой пояс Восточного времени (Нью-Йорк/EDT)
        var edtZone = TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

        // 2. Конвертируем текущее время вашего ПК (МСК) в время EDT
        DateTime edtNow = TimeZoneInfo.ConvertTime(DateTime.Now, edtZone);

        return new IncidentGeneralInfo(
            room: residentInfo.Room,
            bed: residentInfo.Bed,
            date: edtNow.Date, // Передаст текущую дату EDT со временем 00:00:00
            time: new TimeOnly(edtNow.Hour, edtNow.Minute),
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
            AmbulatoryStatus: "Non Ambulatory", // Соответствует тексту у радиокнопки
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

    private static List<MedicationTab.MedicationInfo> CreateDefaultMedications() => new() {
        new ("Aspirin", "100mg", "Once a day", "08:00"),
        new ("Nurafen", "250mg", "Twice a day", "15:00"),
        new ("Melatonin", "5mg", "Before sleep", "21:00")
    };


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

    private static SummaryTab.IncidentSummaryInfo CreateDefaultSummary() => new(
    CarePlanUpdated: true,
    SetAsReportable: true,
    MajorInjury: true,
    SendToLegal: true,
    Summary: "The incident was reviewed by the interdisciplinary team. Patient's condition is stable.",
    Plan: "Continue monitoring vital signs every 4 hours for the next 24 hours and update care plan goals.",
    Conclusion: "Unavoidable", // Значение должно совпадать с текстом у радиокнопки
    EvidenceOfAbuse: false,
    EvidenceReason: " The resident was observed sitting on the floor and the resident was not able to give " +
        "details of what occurred secondary to confusion.  It was concluded from the review of the camera footage " +
        "and staff statements that a fall had occurred in the absence of evidence suggesting otherwise. There were " +
        "no elements suggesting that abuse, neglect, or mistreatment occurred ",
    ReportedToAgency: false,
    PossibleContributingFactor: new[] {"FALLS WITHIN 30 DAYS ADMISSION", "UNDERLYING CHRONIC CONDITION" }, // Выберите существующее значение из вашего Dropdown
    DirectorSignature: "Polly Test" // Имя, которое вводится в поле подписи
);
}