using System;

namespace Salaros.Config.Ini
{
    /// <summary>
    /// Interface for implementing a Logger.
    /// </summary>
    public interface ILoggingService
    {
        #region Settings

        /// <summary>
        /// Gets a value indicating whether this instance is debug enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is debug enabled; otherwise, <c>false</c>.</value>
        bool IsDebugEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is error enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is error enabled; otherwise, <c>false</c>.</value>
        bool IsErrorEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is fatal enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is fatal enabled; otherwise, <c>false</c>.</value>
        bool IsFatalEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is info enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is info enabled; otherwise, <c>false</c>.</value>
        bool IsInfoEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is trace enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is trace enabled; otherwise, <c>false</c>.</value>
        bool IsTraceEnabled { get; }

        /// <summary>
        /// Gets a value indicating whether this instance is warn enabled.
        /// </summary>
        /// <value><c>true</c> if this instance is warn enabled; otherwise, <c>false</c>.</value>
        bool IsWarnEnabled { get; }

        #endregion

        #region Trace

        /// <summary>
        /// Trace the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Trace(Exception exception);

        /// <summary>
        /// Trace the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Trace(string format, params object[] args);

        /// <summary>
        /// Trace the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Trace(Exception exception, string format, params object[] args);

        #endregion

        #region Debug

        /// <summary>
        /// Debug the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Debug(Exception exception);

        /// <summary>
        /// Debug the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Debug(string format, params object[] args);

        /// <summary>
        /// Debug the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Debug(Exception exception, string format, params object[] args);

        #endregion

        #region Info

        /// <summary>
        /// Info the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Info(Exception exception);

        /// <summary>
        /// Info the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Info(string format, params object[] args);

        /// <summary>
        /// Info the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Info(Exception exception, string format, params object[] args);

        #endregion

        #region Warn

        /// <summary>
        /// Warn the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Warn(Exception exception);

        /// <summary>
        /// Warn the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Warn(string format, params object[] args);

        /// <summary>
        /// Warn the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Warn(Exception exception, string format, params object[] args);

        #endregion

        #region Error

        /// <summary>
        /// Error the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Error(Exception exception);

        /// <summary>
        /// Error the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Error(string format, params object[] args);

        /// <summary>
        /// Error the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Error(Exception exception, string format, params object[] args);

        #endregion

        #region Fatal

        /// <summary>
        /// Fatal the specified exception.
        /// </summary>
        /// <param name="exception">Exception.</param>
        void Fatal(Exception exception);

        /// <summary>
        /// Fatal the specified format and args.
        /// </summary>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Fatal(string format, params object[] args);

        /// <summary>
        /// Fatal the specified exception, format and args.
        /// </summary>
        /// <param name="exception">Exception.</param>
        /// <param name="format">Format.</param>
        /// <param name="args">Arguments.</param>
        void Fatal(Exception exception, string format, params object[] args);

        #endregion
    }
}

