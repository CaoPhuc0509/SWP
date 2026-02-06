using eyewearshop_data.Entities;

namespace eyewearshop_service.Validation;

public static class RxCompatibility
{
    public static List<string> ValidatePrescriptionAgainstLens(Prescription prescription, RxLensSpec lens)
    {
        var issues = new List<string>();

        static bool OutOfRange(decimal? value, decimal? min, decimal? max)
        {
            if (!value.HasValue) return false; // missing value: don't block
            if (min.HasValue && value.Value < min.Value) return true;
            if (max.HasValue && value.Value > max.Value) return true;
            return false;
        }

        // Sphere
        if (OutOfRange(prescription.RightSphere, lens.MinSphere, lens.MaxSphere))
            issues.Add("Right eye Sphere is outside the Rx lens supported range.");
        if (OutOfRange(prescription.LeftSphere, lens.MinSphere, lens.MaxSphere))
            issues.Add("Left eye Sphere is outside the Rx lens supported range.");

        // Cylinder
        if (OutOfRange(prescription.RightCylinder, lens.MinCylinder, lens.MaxCylinder))
            issues.Add("Right eye Cylinder is outside the Rx lens supported range.");
        if (OutOfRange(prescription.LeftCylinder, lens.MinCylinder, lens.MaxCylinder))
            issues.Add("Left eye Cylinder is outside the Rx lens supported range.");

        // Axis (only validate if provided in prescription)
        if (OutOfRange(prescription.RightAxis, lens.MinAxis, lens.MaxAxis))
            issues.Add("Right eye Axis is outside the Rx lens supported range.");
        if (OutOfRange(prescription.LeftAxis, lens.MinAxis, lens.MaxAxis))
            issues.Add("Left eye Axis is outside the Rx lens supported range.");

        // Add (progressive/bifocal)
        if (OutOfRange(prescription.RightAdd, lens.MinAdd, lens.MaxAdd))
            issues.Add("Right eye Add power is outside the Rx lens supported range.");
        if (OutOfRange(prescription.LeftAdd, lens.MinAdd, lens.MaxAdd))
            issues.Add("Left eye Add power is outside the Rx lens supported range.");

        return issues;
    }

    public static List<string> ValidateFrameLensCompatibility(FrameSpec frame, RxLensSpec lens)
    {
        var issues = new List<string>();

        // Basic cut size check (very simplified): lens blank width must cover frame A size
        if (lens.LensWidth.HasValue && frame.A.HasValue && frame.A.Value > lens.LensWidth.Value)
            issues.Add("Rx lens blank width is smaller than the frame A size (cannot cut lens to fit frame).");

        // Rimless drilling compatibility (simplified heuristic based on material name)
        if (IsRimless(frame))
        {
            var material = (lens.Material ?? "").ToUpperInvariant();
            var drillSafe = material.Contains("TRIVEX") ||
                            material.Contains("POLYCARB") ||
                            material.Contains("POLYCARBONATE");

            if (!drillSafe)
                issues.Add("Rimless frames require drill-safe lenses (e.g., Trivex or Polycarbonate).");
        }

        return issues;
    }

    private static bool IsRimless(FrameSpec frame)
    {
        var rimType = (frame.RimType ?? "").ToUpperInvariant();
        return rimType.Contains("RIMLESS");
    }
}

