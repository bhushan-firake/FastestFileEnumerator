using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

namespace FastestEnumerator
{
    /// <summary>
    /// Provides the implementation of the 
    /// <see cref="T:System.Collections.Generic.IEnumerator`1"/> interface
    /// </summary>
    [SuppressUnmanagedCodeSecurity]
    public class FileEnumerator : IEnumerator<FileData>
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern SafeFindHandle FindFirstFile(string fileName,
            [In, Out] WIN32_FIND_DATA data);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool FindNextFile(SafeFindHandle hndFindFile,
                [In, Out, MarshalAs(UnmanagedType.LPStruct)] WIN32_FIND_DATA lpFindFileData);

        /// <summary>
        /// Hold context information about where we current are in the directory search.
        /// </summary>
        private class SearchContext
        {
            public readonly string Path;
            public Stack<string> SubdirectoriesToProcess;

            public SearchContext(string path)
            {
                this.Path = path;
            }
        }

        private string m_path;
        private string m_filter;
        private SearchOption m_searchOption;
        private Stack<SearchContext> m_contextStack;
        private SearchContext m_currentContext;

        private SafeFindHandle m_hndFindFile;
        private WIN32_FIND_DATA m_win_find_data = new WIN32_FIND_DATA();

        /// <summary>
        /// Initializes a new instance of the <see cref="FileEnumerator"/> class.
        /// </summary>
        /// <param name="path">The path to search.</param>
        /// <param name="filter">The search string to match against files in the path.</param>
        /// <param name="searchOption">
        /// One of the SearchOption values that specifies whether the search 
        /// operation should include all subdirectories or only the current directory.
        /// </param>
        public FileEnumerator(string path, string filter, SearchOption searchOption)
        {
            m_path = path;
            m_filter = filter;
            m_searchOption = searchOption;
            m_currentContext = new SearchContext(path);

            if (m_searchOption == SearchOption.AllDirectories)
            {
                m_contextStack = new Stack<SearchContext>();
            }
        }

        #region IEnumerator<FileData> Members

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        public FileData Current
        {
            get { return new FileData(m_path, m_win_find_data); }
        }

        #endregion

        #region IDisposable Members

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, 
        /// or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            if (m_hndFindFile != null)
            {
                m_hndFindFile.Dispose();
            }
        }

        #endregion

        #region IEnumerator Members

        /// <summary>
        /// Gets the element in the collection at the current position of the enumerator.
        /// </summary>
        /// <value></value>
        /// <returns>
        /// The element in the collection at the current position of the enumerator.
        /// </returns>
        object System.Collections.IEnumerator.Current
        {
            get { return new FileData(m_path, m_win_find_data); }
        }

        /// <summary>
        /// Advances the enumerator to the next element of the collection.
        /// </summary>
        /// <returns>
        /// true if the enumerator was successfully advanced to the next element; 
        /// false if the enumerator has passed the end of the collection.
        /// </returns>
        /// <exception cref="T:System.InvalidOperationException">
        /// The collection was modified after the enumerator was created.
        /// </exception>
        public bool MoveNext()
        {
            bool retval = false;

            //If the handle is null, this is first call to MoveNext in the current 
            // directory.  In that case, start a new search.
            if (m_currentContext.SubdirectoriesToProcess == null)
            {
                if (m_hndFindFile == null)
                {
                    new FileIOPermission(FileIOPermissionAccess.PathDiscovery, m_path).Demand();

                    string searchPath = Path.Combine(m_path, m_filter);
                    m_hndFindFile = FindFirstFile(searchPath, m_win_find_data);
                    retval = !m_hndFindFile.IsInvalid;
                }
                else
                {
                    //Otherwise, find the next item.
                    retval = FindNextFile(m_hndFindFile, m_win_find_data);
                }
            }

            //If the call to FindNextFile or FindFirstFile succeeded...
            if (retval)
            {
                if (((FileAttributes)m_win_find_data.dwFileAttributes & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    //Ignore folders for now.   We call MoveNext recursively here to 
                    // move to the next item that FindNextFile will return.
                    return MoveNext();
                }
            }
            else if (m_searchOption == SearchOption.AllDirectories)
            {
                //SearchContext context = new SearchContext(m_hndFindFile, m_path);
                //m_contextStack.Push(context);
                //m_path = Path.Combine(m_path, m_win_find_data.cFileName);
                //m_hndFindFile = null;

                if (m_currentContext.SubdirectoriesToProcess == null)
                {
                    string[] subDirectories = Directory.GetDirectories(m_path);
                    m_currentContext.SubdirectoriesToProcess = new Stack<string>(subDirectories);
                }

                if (m_currentContext.SubdirectoriesToProcess.Count > 0)
                {
                    string subDir = m_currentContext.SubdirectoriesToProcess.Pop();

                    m_contextStack.Push(m_currentContext);
                    m_path = subDir;
                    m_hndFindFile = null;
                    m_currentContext = new SearchContext(m_path);
                    return MoveNext();
                }

                //If there are no more files in this directory and we are 
                // in a sub directory, pop back up to the parent directory and
                // continue the search from there.
                if (m_contextStack.Count > 0)
                {
                    m_currentContext = m_contextStack.Pop();
                    m_path = m_currentContext.Path;
                    if (m_hndFindFile != null)
                    {
                        m_hndFindFile.Close();
                        m_hndFindFile = null;
                    }

                    return MoveNext();
                }
            }

            return retval;
        }

        /// <summary>
        /// Sets the enumerator to its initial position, which is before the first element in the collection.
        /// </summary>
        /// <exception cref="T:System.InvalidOperationException">
        /// The collection was modified after the enumerator was created.
        /// </exception>
        public void Reset()
        {
            m_hndFindFile = null;
        }

        #endregion
    }
}
