namespace ChuckDeviceController.Net.Models;

using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum KojiReturnType
{
    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns a space (` `) delimited string of coordinates.</returns>
    /// <example>
    /// <code>lat lon,lat lon</code>
    /// </example>
    [Display(GroupName = "Text Format", Name = "AltText", Description = "")]
    AltText, // alttext | alt_text | alt-text

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns a line feed/newline (`\n`) delimited string of coordinates.</returns>
    /// <example>
    /// <code>lat,lon\nlat,lon</code>
    /// </example>
    [Display(GroupName = "Text Format", Name = "Text", Description = "")]
    Text, // text

    /// <summary>
    /// 
    /// </summary>
    /// <returns>
    /// Returns an array, the inner array contains the coordinates.
    /// <code>
    /// [
    ///     [lat, lon],
    ///     [lat, lon],
    ///     etc...
    /// ]</code>
    /// </returns>
    [Display(GroupName = "Array Format", Name = "Single Array", Description = "")]
    SingleArray, // singlearray | single_array | single-array

    /// <summary>
    /// 
    /// </summary>
    /// <returns>
    /// Returns a 2-dimensional array, the inner array contains the coordinates.
    /// </returns>
    /// <example>
    /// <code>
    /// [
    ///   [
    ///     [lat, lon],
    ///     [lat, lon],
    ///     etc...
    ///   ],
    ///   [
    ///     [lat, lon],
    ///     [lat, lon],
    ///     etc...
    ///   ],
    ///   etc...
    /// ]
    /// </code>
    /// </example>
    [Display(GroupName = "Array Format", Name = "Multi Array", Description = "")]
    MultiArray, // multiarray | multi_array | multi-array

    /// <summary>
    /// 
    /// </summary>
    /// <returns>
    /// Returns an array of coordinate objects.
    /// </returns>
    /// <example>
    /// [
    ///   { "lat": 0, "lon": 0 },
    ///   { "lat": 1, "lon": 1 },
    ///   etc...
    /// ]
    /// </example>
    [Display(GroupName = "Structured Format", Name = "Single Structured Array", Description = "")]
    SingleStruct, // singlestruct | single_struct | single-struct

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns a 2-dimensional array of coordinate objects.</returns>
    /// <example>
    /// <code>
    /// [
    ///   [
    ///     { "lat": 0, "lon": 0 },
    ///     { "lat": 1, "lon": 1 },
    ///     etc...
    ///   ],
    ///   [
    ///     { "lat": 0, "lon": 0 },
    ///     { "lat": 1, "lon": 1 },
    ///     etc...
    ///   ],
    ///   etc...
    /// ]
    /// </code>
    /// </example>
    [Display(GroupName = "Structured Format", Name = "Multi Structured Array", Description = "")]
    MultiStruct, // multistruct | multi_struct | multi-struct

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns a single GeoJSON formatted Feature.</returns>
    /// <example>
    /// <code>
    /// {
    ///   "geometry": {
    ///     "bbox": [
    ///       min_lon,
    ///       min_lat,
    ///       max_lon,
    ///       max_lat
    ///     ],
    ///     "coordinates": [
    ///       [
    ///         [
    ///           [ lon, lat ],
    ///           [ lon, lat ],
    ///           etc...
    ///         ],
    ///         [
    ///           [ lon, lat ],
    ///           [ lon, lat ],
    ///           etc...
    ///         ],
    ///         etc...
    ///       ]
    ///     ],
    ///     "type": "MultiPolygon"
    ///   },
    ///   "properties": {
    ///     "name": "InstanceName",
    ///     "type": "InstanceType"
    ///   },
    ///   "type": "Feature"
    /// }
    /// </code>
    /// </example>
    [Display(GroupName = "GeoJSON Format", Name = "Single GeoJSON", Description = "")]
    Feature, // feature

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns an array of GeoJSON formatted Features.</returns>
    /// <example>
    /// <code>
    /// [{
    ///   "geometry": {
    ///     "bbox": [
    ///       min_lon,
    ///       min_lat,
    ///       max_lon,
    ///       max_lat
    ///     ],
    ///     "coordinates": [
    ///       [
    ///         [
    ///           [ lon, lat ],
    ///           [ lon, lat ],
    ///           etc...
    ///         ],
    ///         [
    ///           [ lon, lat ],
    ///           [ lon, lat ],
    ///           etc...
    ///         ],
    ///         etc...
    ///       ]
    ///     ],
    ///     "type": "MultiPolygon"
    ///   },
    ///   "properties": {
    ///     "name": "InstanceName",
    ///     "type": "InstanceType"
    ///   },
    ///   "type": "Feature"
    /// }],
    /// [
    ///   etc...
    /// ]
    /// </code>
    /// </example>
    [Display(GroupName = "GeoJSON Format", Name = "", Description = "")]
    FeatureVec, // featurevec | feature_vec | feature-vec

    /// <summary>
    /// 
    /// </summary>
    /// <returns>Returns a collection of GeoJSON formatted Features.</returns>
    /// <example>
    /// <code>
    /// {
    ///   "bbox": [
    ///     min_lon,
    ///     min_lat,
    ///     max_lon,
    ///     max_lat
    ///   ],
    ///   "features": [{
    ///     "geometry": {
    ///       "bbox": [
    ///         min_lon,
    ///         min_lat,
    ///         max_lon,
    ///         max_lat
    ///       ],
    ///       "coordinates": [
    ///         [
    ///           [
    ///             [ lon, lat ],
    ///             [ lon, lat ],
    ///             etc...
    ///           ],
    ///           [
    ///             [ lon, lat ],
    ///             [ lon, lat ],
    ///             etc...
    ///           ],
    ///           etc...
    ///         ]
    ///       ],
    ///       "type": "MultiPolygon"
    ///     },
    ///     "properties": {
    ///       "name": "InstanceName",
    ///       "type": InstanceType"
    ///     },
    ///     "type": "Feature"
    ///   },{
    ///     etc...
    ///   }],
    ///   "type": "FeatureCollection"
    /// }
    /// </code>
    /// </example>
    [Display(GroupName = "GeoJSON Format", Name = "Multi GeoJSON", Description = "")]
    FeatureCollection, // featurecollection | feature_collection | feature-collection

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    /// <example>
    /// <code>
    /// [{
    ///   "displayInMatches": true,
    ///   "id": 0,
    ///   "multipath": [
    ///     [
    ///       [ lat, lon ],
    ///       [ lat, lon ],
    ///       [ lat, lon ],
    ///       etc...
    ///     ],
    ///     etc...
    ///   ],
    ///   "name": "InstanceName",
    ///   "userSelectable": true
    /// },{
    ///   etc...
    /// }]
    /// </code>
    /// </example>
    [Display(GroupName = "GeoJSON Format", Name = "", Description = "")]
    Poracle, // poracle
}

/*
pub fn get_return_type(return_type: String, default_return_type: &ReturnTypeArg) -> ReturnTypeArg {
    match return_type.to_lowercase().as_str() {
        "alttext" | "alt_text" | "alt-text" => ReturnTypeArg::AltText,
        "text" => ReturnTypeArg::Text,
        "array" => match *default_return_type {
            ReturnTypeArg::SingleArray => ReturnTypeArg::SingleArray,
            ReturnTypeArg::MultiArray => ReturnTypeArg::MultiArray,
            _ => ReturnTypeArg::SingleArray,
        },
        "singlearray" | "single_array" | "single-array" => ReturnTypeArg::SingleArray,
        "multiarray" | "multi_array" | "multi-array" => ReturnTypeArg::MultiArray,
        "struct" => match *default_return_type {
            ReturnTypeArg::SingleStruct => ReturnTypeArg::SingleStruct,
            ReturnTypeArg::MultiStruct => ReturnTypeArg::MultiStruct,
            _ => ReturnTypeArg::SingleStruct,
        },
        "singlestruct" | "single_struct" | "single-struct" => ReturnTypeArg::SingleStruct,
        "multistruct" | "multi_struct" | "multi-struct" => ReturnTypeArg::MultiStruct,
        "feature" => ReturnTypeArg::Feature,
        "featurevec" | "feature_vec" | "feature-vec" => ReturnTypeArg::FeatureVec,
        "poracle" => ReturnTypeArg::Poracle,
        "featurecollection" | "feature_collection" | "feature-collection" => {
            ReturnTypeArg::FeatureCollection
        }
        _ => default_return_type.clone(),
    }
}
 */