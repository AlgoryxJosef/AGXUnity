﻿using System;
using System.Linq;
using UnityEngine;

namespace AGXUnity
{
  /// <summary>
  /// Contact listener data used by the ContactEventHandler.
  /// </summary>
  public class ContactListener
  {
    /// <summary>
    /// Components this contact listener is listening to.
    /// </summary>
    public ScriptComponent[] Components { get; private set; } = null;

    /// <summary>
    /// Callback taking ContactData for a matching component.
    /// </summary>
    public ContactEventHandler.OnContactDelegate Callback { get; private set; } = null;

    /// <summary>
    /// Native filter matching contacts in the simulation.
    /// </summary>
    public agxSDK.UuidHashCollisionFilter Filter { get; private set; } = null;

    /// <summary>
    /// True if removed, otherwise false.
    /// </summary>
    public bool IsRemoved { get; set; } = false;

    /// <summary>
    /// True if this listener is listening to contacts before the contact
    /// is solved.
    /// </summary>
    public bool OnContactEnabled
    {
      get
      {
        return ( m_activationMask & agxSDK.ContactEventListener.ActivationMask.CONTACT ) != 0;
      }
    }

    /// <summary>
    /// True if this listener is listening to contact forces of matching contacts.
    /// </summary>
    public bool OnForceEnabled
    {
      get
      {
        return ( m_activationMask & agxSDK.ContactEventListener.ActivationMask.POST ) != 0;
      }
    }

    /// <summary>
    /// Contact filtering mode of this filter.
    /// </summary>
    public agxSDK.UuidHashCollisionFilter.Mode FilteringMode
    {
      get { return m_filteringMode; }
      set
      {
        m_filteringMode = value;
        if ( Filter != null )
          Filter.setMode( m_filteringMode );
      }
    }

    /// <summary>
    /// Construct given callback, components and activation mask. If this listener
    /// is listening to OnContact, activation mask should be:
    ///     agxSDK.ContactEventListener.ActivationMask.IMPACT | agxSDK.ContactEventListener.ActivationMask.CONTACT
    /// If this listener is listening to contact forces only, activation mask should be:
    ///     agxSDK.ContactEventListener.ActivationMask.POST
    /// </summary>
    /// <param name="callback">Callback when a ContactData is available for a matching component.</param>
    /// <param name="components">Components to match for contacts.</param>
    /// <param name="activationMask">Activation mask.</param>
    public ContactListener( ContactEventHandler.OnContactDelegate callback,
                            ScriptComponent[] components,
                            agxSDK.ContactEventListener.ActivationMask activationMask )
    {
      Callback = callback ?? throw new ArgumentNullException( "callback" );
      Components = components ?? throw new ArgumentNullException( "components" );
      m_activationMask = activationMask;
      FilteringMode = Components.Length == 0 ?
                        agxSDK.UuidHashCollisionFilter.Mode.MATCH_ALL :
                      Components.Length == 1 ?
                        agxSDK.UuidHashCollisionFilter.Mode.MATCH_OR :
                        agxSDK.UuidHashCollisionFilter.Mode.MATCH_AND;
    }

    /// <summary>
    /// Remove the given component with the given UUID from this listener.
    /// </summary>
    /// <param name="uuid">Native UUID of the given component.</param>
    /// <param name="component">Component to remove.</param>
    /// <param name="notifyOnRemove">True to log info about remove status.</param>
    /// <returns>
    /// True if this listener becomes invalid after successfully removing
    /// the component and should be removed.
    /// </returns>
    public bool Remove( uint uuid, ScriptComponent component, bool notifyOnRemove = true )
    {
      if ( Filter != null ) {
        if ( !Filter.contains( uuid ) )
          return false;
        Filter.remove( uuid );
      }

      var index = Array.IndexOf( Components, component );
      if ( index < 0 )
        return false;

      Components = Array.FindAll( Components, c => c != component );

      // Remove us if we had one component and now zero, which disqualifies us from MATCH_OR.
      // Remove us if we had two or more components and now less than two, which disqualifies us from MATCH_AND.
      var removeMe = Filter == null ||
                     ( Filter.getMode() == agxSDK.UuidHashCollisionFilter.Mode.MATCH_OR && Components.Length == 0 ) ||
                     ( Filter.getMode() == agxSDK.UuidHashCollisionFilter.Mode.MATCH_AND && Components.Length < 2 );

      if ( removeMe && notifyOnRemove )
        Debug.Log( $"AGXUnity.ContactListener: Removing contact callback {ContactEventHandler.FindCallbackName( Callback )} due " +
                   $"to remove of component {component} with UUID {uuid}." );

      return removeMe;
    }

    /// <summary>
    /// Finds UUIDs of our components and registers the filter to the simulation.
    /// </summary>
    /// <param name="handler">A contact event handler.</param>
    public void Initialize( ContactEventHandler handler )
    {
      if ( Filter != null )
        return;

      Filter = new agxSDK.UuidHashCollisionFilter();
      Filter.setMode( FilteringMode );
      foreach ( var component in Components ) {
        // TODO: This should be a GetOrCreate where handler is assigning UUID.
        var uuid = handler.GetUuid( component );
        if ( uuid == 0u ) {
          Debug.LogWarning( $"AGXUnity.ContactEventHandler: Unknown unique simulation id for component of type {component.GetType().FullName} - " +
                            $"it's not possible match contacts without identifier, ignoring component.",
                            component );
          continue;
        }

        Filter.add( uuid );
      }

      handler.GeometryContactHandler.Native.add( Filter, (int)m_activationMask );
    }

    /// <summary>
    /// Disposes the filter and removes listener from the simulation.
    /// </summary>
    /// <param name="handler">The contact event handler this listener was added to.</param>
    /// <returns>True.</returns>
    public bool OnDestroy( ContactEventHandler handler )
    {
      if ( Filter != null && handler.GeometryContactHandler.Native != null )
        handler.GeometryContactHandler.Native.remove( Filter );

      Filter?.Dispose();
      Filter = null;

      return true;
    }

    public void SetActivationMask( agxSDK.MultiCollisionFilterContactHandler handler,
                                   agxSDK.ContactEventListener.ActivationMask activationMask )
    {
      m_activationMask = activationMask;
      if ( handler != null && Filter != null )
        handler.setActivationMask( Filter, (int)m_activationMask );
    }

    private agxSDK.ContactEventListener.ActivationMask m_activationMask;
    private agxSDK.UuidHashCollisionFilter.Mode m_filteringMode = agxSDK.UuidHashCollisionFilter.Mode.MATCH_ALL;
  }
}
