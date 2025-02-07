//MIT license

using System.Collections.Specialized;

namespace System.Web.UI.WebControls;

/// <devdoc>
/// <para>Provides data for some <see cref='System.Web.UI.WebControls.GridView'/> events.</para>
/// </devdoc>

public class GridViewDeletedEventArgs : EventArgs {

    private int _affectedRows;
    private Exception _exception;
    private bool _exceptionHandled;
    private IOrderedDictionary _keys;
    private IOrderedDictionary _values;

    /// <devdoc>
    /// <para>Initializes a new instance of the <see cref='System.Web.UI.WebControls.GridViewDeletedEventArgs'/>
    /// class.</para>
    /// </devdoc>
    public GridViewDeletedEventArgs(int affectedRows, Exception e) {
        this._affectedRows = affectedRows;
        this._exceptionHandled = false;
        this._exception = e;
    }

    /// <devdoc>
    ///    <para>Gets the source of the command. This property is read-only.</para>
    /// </devdoc>
    public int AffectedRows {
        get {
            return _affectedRows;
        }
    }

    /// <devdoc>
    ///    <para>Gets the exception (if any) that occurred during the operation. This property is read-only.</para>
    /// </devdoc>
    public Exception Exception {
        get {
            return _exception;
        }
    }

    /// <devdoc>
    ///    <para>Gets a flag telling whether the exception was handled.</para>
    /// </devdoc>
    public bool ExceptionHandled {
        get {
            return _exceptionHandled;
        }
        set {
            _exceptionHandled = value;
        }
    }

    /// <devdoc>
    /// <para>Gets a keyed list to populate with parameters that identify the row to delete.  This property is read-only.</para>
    /// </devdoc>
    public IOrderedDictionary Keys {
        get {
            if (_keys == null) {
                _keys = new OrderedDictionary();
            }
            return _keys;
        }
    }

    /// <devdoc>
    /// <para>Gets a keyed list to populate with old row values.  This property is read-only.</para>
    /// </devdoc>
    public IOrderedDictionary Values {
        get {
            if (_values == null) {
                _values = new OrderedDictionary();
            }
            return _values;
        }
    }

    internal void SetKeys(IOrderedDictionary keys) {
        _keys = keys;
    }
    
    internal void SetValues(IOrderedDictionary values) {
        _values = values;
    }
}

