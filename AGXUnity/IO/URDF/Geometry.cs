﻿using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace AGXUnity.IO.URDF
{
  /// <summary>
  /// Required element "geometry" for optional elements "collision" and "visual".
  /// This element reads:
  ///   - Either required element; "box", "cylinder", "sphere" or "mesh".
  ///     - "box":      Required attribute "size".
  ///     - "cylinder": Required attributes "radius" and "length".
  ///     - "sphere":   Required attribute "radius".
  ///     - "mesh":     Required attribute "filename" and optional attribute "scale" (default [1, 1, 1]).
  /// </summary>
  /// <remarks>
  /// This class will throw an UrdfIOException when used properties doesn't match
  /// the geometry type read. E.g., will throw when Type == GeometryType.Box
  /// and Geometry.Radius is used.
  /// </remarks>
  [DoNotGenerateCustomEditor]
  public class Geometry : Element
  {
    /// <summary>
    /// Reads data where "geometry" is required. Throws an UrdfIOException
    /// if "geometry" isn't an element to given <paramref name="parent"/>.
    /// </summary>
    /// <param name="parent">Parent element "visual" or "collision".</param>
    /// <returns>Read geometry instance.</returns>
    public static Geometry ReadRequired( XElement parent )
    {
      var geometryElement = parent.Element( "geometry" );
      if ( geometryElement == null )
        throw new UrdfIOException( $"{Utils.GetLineInfo( parent )}: {parent.Name} doesn't contain required 'geometry'." );
      return Instantiate<Geometry>( geometryElement );
    }

    /// <summary>
    /// Geometry types given specification.
    /// </summary>
    public enum GeometryType
    {
      /// <summary>
      /// Geometry type "box" with required attribute "size" => FullExtents.
      /// </summary>
      Box,
      /// <summary>
      /// Geometry type "cylinder" with required attributes "radius" => Radius and "length" => Length.
      /// </summary>
      Cylinder,
      /// <summary>
      /// Geometry type "sphere" with required attribute "radius" => Radius.
      /// </summary>
      Sphere,
      /// <summary>
      /// Geometry type "mesh" with required attribute "filename" => Filename and
      /// optional attribute "scale" => Scale (default [1, 1, 1]).
      /// </summary>
      Mesh,
      Unknown
    }

    /// <summary>
    /// GeometryType.Mesh scale.
    /// </summary>
    public Vector3 Scale
    {
      get
      {
        if ( Type != GeometryType.Mesh )
          throw new UrdfIOException( $"Asking scale of GeometryType {Type} is undefined." );
        return m_size;
      }
      set
      {
        if ( Type != GeometryType.Mesh )
          throw new UrdfIOException( $"Asking scale of GeometryType {Type} is undefined." );
        m_size = value;
      }
    }

    /// <summary>
    /// GeometryType.Mesh filename.
    /// </summary>
    public string Filename
    {
      get
      {
        if ( Type != GeometryType.Mesh )
          throw new UrdfIOException( $"Asking filename of GeometryType {Type} is undefined." );
        return m_filename;
      }
      set
      {
        if ( Type != GeometryType.Mesh )
          throw new UrdfIOException( $"Asking filename of GeometryType {Type} is undefined." );
        m_filename = value;
      }
    }

    /// <summary>
    /// GeometryType.Box full extents (size).
    /// </summary>
    public Vector3 FullExtents
    {
      get
      {
        if ( Type != GeometryType.Box )
          throw new UrdfIOException( $"Asking extents of GeometryType {Type} is undefined." );
        return m_size;
      }
      set
      {
        if ( Type != GeometryType.Box )
          throw new UrdfIOException( $"Asking extents of GeometryType {Type} is undefined." );
        m_size = value;
      }
    }

    /// <summary>
    /// GeometryType.Cylinder and GeometryType.Sphere radius.
    /// </summary>
    public float Radius
    {
      get
      {
        if ( Type != GeometryType.Cylinder && Type != GeometryType.Sphere )
          throw new UrdfIOException( $"Asking radius of GeometryType {Type} is undefined." );
        return m_size[ 0 ];
      }
      set
      {
        if ( Type != GeometryType.Cylinder && Type != GeometryType.Sphere )
          throw new UrdfIOException( $"Asking radius of GeometryType {Type} is undefined." );
        m_size[ 0 ] = value;
      }
    }

    /// <summary>
    /// GeometryType.Cylinder length.
    /// </summary>
    public float Length
    {
      get
      {
        if ( Type != GeometryType.Cylinder )
          throw new UrdfIOException( $"Asking length of GeometryType {Type} is undefined." );
        return m_size[ 1 ];
      }
      set
      {
        if ( Type != GeometryType.Cylinder )
          throw new UrdfIOException( $"Asking length of GeometryType {Type} is undefined." );
        m_size[ 1 ] = value;
      }
    }

    /// <summary>
    /// Type of this geometry.
    /// </summary>
    public GeometryType Type { get { return m_type; } private set { m_type = value; } }

    /// <summary>
    /// Reads required element "geometry".
    /// </summary>
    /// <param name="element">Required "geometry" element - invalid if null.</param>
    /// <param name="optional">Unused.</param>
    public override void Read( XElement element, bool optional = true )
    {
      var children = element.Elements().ToArray();
      if ( children.Length != 1 )
        throw new UrdfIOException( $"{Utils.GetLineInfo( element )}: Invalid 'geometry' - expecting 1 geometry type, got {children.Length}." );
      Type = children[ 0 ].Name == "box" ?
               GeometryType.Box :
             children[ 0 ].Name == "cylinder" ?
               GeometryType.Cylinder :
             children[ 0 ].Name == "sphere" ?
               GeometryType.Sphere :
             children[ 0 ].Name == "mesh" ?
               GeometryType.Mesh :
               GeometryType.Unknown;
      if ( Type == GeometryType.Unknown )
        throw new UrdfIOException( $"{Utils.GetLineInfo( children[ 0 ] )}: Unknown geometry type '{children[ 0 ].Name}'." );
      if ( Type == GeometryType.Box )
        FullExtents = Utils.ReadVector3( children[ 0 ], "size", false );
      else if ( Type == GeometryType.Cylinder ) {
        Radius = Utils.ReadFloat( children[ 0 ], "radius", false );
        Length = Utils.ReadFloat( children[ 0 ], "length", false );
      }
      else if ( Type == GeometryType.Sphere )
        Radius = Utils.ReadFloat( children[ 0 ], "radius", false );
      else {
        Filename = Utils.ReadString( children[ 0 ], "filename", false );
        Scale    = children[ 0 ].Attribute( "scale" ) != null ?
                     Utils.ReadVector3( children[ 0 ], "scale" ) :
                     Vector3.one;
      }
    }

    [SerializeField]
    private Vector3 m_size = Vector3.zero;

    [SerializeField]
    private string m_filename = string.Empty;

    [SerializeField]
    private GeometryType m_type = GeometryType.Unknown;
  }
}
