
using UnityEngine;
using Unity.Profiling;
using System;
using System.Collections.Generic;

public abstract class DataPoint {
    public int frame;
    public abstract string GetCSV();
}

public class FrameDurationDataPoint : DataPoint {
    public double duration;
    public override string ToString() {
        return frame + " | FrameDuration: duration: " + duration + " ms";
    }

    public static string GetCSVHeader() {
        return "Frame,Duration,Fps";
    }

    public override string GetCSV() {
        return frame + "," + duration + "," + (1000.0 / duration);
    }
}

public class CelestialObjectProfiler : MonoBehaviour {
    void OnEnable() {
        
    }

    void OnDisable() {
        // Create output log file and write results
        string frame_duration_file_name = "CelestialObjectProfiler_FrameDuration_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".csv";
        string frame_duration_file_path = System.Environment.CurrentDirectory + "/" + frame_duration_file_name;
        System.IO.File.WriteAllText(frame_duration_file_path, FrameDurationDataPoint.GetCSVHeader() + "\n");
        foreach (DataPoint data_point in dataPoints) {
            if(data_point is FrameDurationDataPoint)
                System.IO.File.AppendAllText(frame_duration_file_path, data_point.GetCSV() + "\n");
        }
    }

    void Update() {
        FrameDurationDataPoint frame_duration_data_point = new FrameDurationDataPoint();
        frame_duration_data_point.frame = Time.frameCount;
        
        FrameTiming[] frameTimings = new FrameTiming[1];
        FrameTimingManager.GetLatestTimings(1, frameTimings);
        frame_duration_data_point.duration = frameTimings[0].gpuFrameTime;

        AddDataPoint(frame_duration_data_point);
    }

    public void AddDataPoint(DataPoint data_point) {
        dataPoints.Add(data_point);
    }

    public List<DataPoint> dataPoints = new List<DataPoint>();
}
