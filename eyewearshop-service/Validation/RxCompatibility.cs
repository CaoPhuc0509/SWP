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
            issues.Add("Độ cận/loạn (Sphere) mắt phải nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");
        if (OutOfRange(prescription.LeftSphere, lens.MinSphere, lens.MaxSphere))
            issues.Add("Độ cận/loạn (Sphere) mắt trái nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");

        // Cylinder
        if (OutOfRange(prescription.RightCylinder, lens.MinCylinder, lens.MaxCylinder))
            issues.Add("Độ loạn (Cylinder) mắt phải nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");
        if (OutOfRange(prescription.LeftCylinder, lens.MinCylinder, lens.MaxCylinder))
            issues.Add("Độ loạn (Cylinder) mắt trái nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");

        // Axis (only validate if provided in prescription)
        if (OutOfRange(prescription.RightAxis, lens.MinAxis, lens.MaxAxis))
            issues.Add("Trục loạn (Axis) mắt phải nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");
        if (OutOfRange(prescription.LeftAxis, lens.MinAxis, lens.MaxAxis))
            issues.Add("Trục loạn (Axis) mắt trái nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");

        // Add (progressive/bifocal)
        if (OutOfRange(prescription.RightAdd, lens.MinAdd, lens.MaxAdd))
            issues.Add("Độ cộng thêm (Add) mắt phải nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");
        if (OutOfRange(prescription.LeftAdd, lens.MinAdd, lens.MaxAdd))
            issues.Add("Độ cộng thêm (Add) mắt trái nằm ngoài phạm vi hỗ trợ của tròng kính đã chọn.");

        return issues;
    }

    public static List<string> ValidateFrameLensCompatibility(FrameSpec frame, RxLensSpec lens)
    {
        var issues = new List<string>();

        // Basic cut size check (very simplified): lens blank width must cover frame A size
        if (lens.LensWidth.HasValue && frame.A.HasValue && frame.A.Value > lens.LensWidth.Value)
            issues.Add("Tròng kính đã chọn không phù hợp với kích thước gọng vì đường kính phôi tròng nhỏ hơn size A của gọng.");

        // Rimless drilling compatibility (simplified heuristic based on material name)
        if (IsRimless(frame))
        {
            var material = (lens.Material ?? "").ToUpperInvariant();
            var drillSafe = material.Contains("TRIVEX") ||
                            material.Contains("POLYCARB") ||
                            material.Contains("POLYCARBONATE");

            if (!drillSafe)
                issues.Add("Gọng khoan/rimless cần loại tròng phù hợp để khoan lắp, như Trivex hoặc Polycarbonate.");
        }

        return issues;
    }

    private static bool IsRimless(FrameSpec frame)
    {
        var rimType = (frame.RimType ?? "").ToUpperInvariant();
        return rimType.Contains("RIMLESS");
    }
}