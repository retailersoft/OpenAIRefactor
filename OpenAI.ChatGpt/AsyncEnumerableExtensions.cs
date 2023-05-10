using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpenAI.ChatGpt
{
    public static class AsyncEnumerableExtensions
    {
        internal static async Task<IEnumerable<T>> ConfigureExceptions<T>(
            this IAsyncEnumerable<T> stream,
            bool throwOnCancellation) where T : class
        {
            if (stream == null) throw new ArgumentNullException();
            var enumerator = stream.GetAsyncEnumerator();
            var results = new List<T>();
            T result = null;
            var hasResult = true;
            while (hasResult)
            {
                try
                {
                    hasResult = await enumerator.MoveNextAsync();
                    result = hasResult ? enumerator.Current : null;
                }
                catch (OperationCanceledException)
                {
                    if (throwOnCancellation)
                    {
                        await enumerator.DisposeAsync();
                        throw;
                    }
                }
                if (result != null)
                {
                    results.Add(result);
                }
            }

            await enumerator.DisposeAsync();

            return results;
        }

        internal static async Task<IEnumerable<T>> ConfigureExceptions<T>(
            this IAsyncEnumerable<T> stream,
            bool throwOnCancellation,
            Action<Exception> onExceptionBeforeThrowing) where T : class
        {
            if (stream == null) throw new ArgumentNullException();
            IAsyncEnumerator<T> enumerator;
            try
            {
                enumerator = stream.GetAsyncEnumerator();
            }
            catch (Exception e)
            {
                onExceptionBeforeThrowing?.Invoke(e);
                throw;
            }
            var results = new List<T>();
            T result = null;
            var hasResult = true;
            while (hasResult)
            {
                try
                {
                    hasResult = await enumerator.MoveNextAsync();
                    result = hasResult ? enumerator.Current : null;
                }
                catch (OperationCanceledException e)
                {
                    await DisposeAsyncSafe();
                    onExceptionBeforeThrowing?.Invoke(e);
                    if (throwOnCancellation)
                    {
                        throw;
                    }
                }
                if (result != null)
                {
                    results.Add(result);
                }
            }

            await DisposeAsyncSafe();

            return results;

            async Task DisposeAsyncSafe()
            {
                try
                {
                    await enumerator.DisposeAsync();
                }
                catch (Exception e)
                {
                    onExceptionBeforeThrowing?.Invoke(e);
                    throw;
                }
            }
        }
    }

    //public static class AsyncEnumerableExtensions
    //{
    //    internal static async IAsyncEnumerable<T> ConfigureExceptions<T>(
    //        this IAsyncEnumerable<T> stream,
    //        bool throwOnCancellation) where T : class
    //    {
    //        if (stream == null) throw new ArgumentNullException();
    //        var enumerator = stream.GetAsyncEnumerator();
    //        T result = null;
    //        var hasResult = true;
    //        while (hasResult)
    //        {
    //            try
    //            {
    //                hasResult = await enumerator.MoveNextAsync();
    //                result = hasResult ? enumerator.Current : null;
    //            }
    //            catch (OperationCanceledException)
    //            {
    //                if (throwOnCancellation)
    //                {
    //                    await enumerator.DisposeAsync();
    //                    throw;
    //                }
    //            }
    //            if (result != null)
    //            {
    //                yield return result;
    //            }
    //        }

    //        await enumerator.DisposeAsync();
    //    }

    //    internal static async IAsyncEnumerable<T> ConfigureExceptions<T>(
    //        this IAsyncEnumerable<T> stream,
    //        bool throwOnCancellation,
    //        Action<Exception> onExceptionBeforeThrowing) where T : class
    //    {
    //        if (stream == null) throw new ArgumentNullException();
    //        IAsyncEnumerator<T> enumerator;
    //        try
    //        {
    //            enumerator = stream.GetAsyncEnumerator();
    //        }
    //        catch (Exception e)
    //        {
    //            onExceptionBeforeThrowing?.Invoke(e);
    //            throw;
    //        }
    //        T result = null;
    //        var hasResult = true;
    //        while (hasResult)
    //        {
    //            try
    //            {
    //                hasResult = await enumerator.MoveNextAsync();
    //                result = hasResult ? enumerator.Current : null;
    //            }
    //            catch (OperationCanceledException e)
    //            {
    //                await DisposeAsyncSafe();
    //                onExceptionBeforeThrowing?.Invoke(e);
    //                if (throwOnCancellation)
    //                {
    //                    throw;
    //                }
    //            }
    //            if (result != null)
    //            {
    //                yield return result;
    //            }
    //        }

    //        await DisposeAsyncSafe();

    //        async Task DisposeAsyncSafe()
    //        {
    //            try
    //            {
    //                await enumerator.DisposeAsync();
    //            }
    //            catch (Exception e)
    //            {
    //                onExceptionBeforeThrowing?.Invoke(e);
    //                throw;
    //            }
    //        }
    //    }
    //}
}