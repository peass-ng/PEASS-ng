using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using winPEAS.Info.FilesInfo;

namespace winPEAS.Tests
{
    [TestClass]
    public class VelociraptorServerConfigTests
    {
        private sealed class Fixture
        {
            internal readonly string Path = System.IO.Path.Combine(
                System.IO.Path.GetPathRoot(Environment.SystemDirectory), "Program Files", "VelociraptorServer", "server.config.yaml");
            internal byte[] Bytes = Encoding.UTF8.GetBytes(
                "CA:\n  private_key: |\n    -----BEGIN RSA PRIVATE KEY-----\n    private-secret\n    -----END RSA PRIVATE KEY-----\n");
            internal bool Missing;
            internal bool Denied;
            internal bool Reparse;
            internal int OpenCount;

            internal FileAttributes Attributes(string path)
            {
                if (Missing && path == Path) throw new FileNotFoundException();
                if (Reparse && path == System.IO.Path.GetDirectoryName(Path))
                    return FileAttributes.Directory | FileAttributes.ReparsePoint;
                return path == Path ? FileAttributes.Normal : FileAttributes.Directory;
            }

            internal Stream Open(string path)
            {
                OpenCount++;
                if (Denied) throw new UnauthorizedAccessException();
                return new MemoryStream(Bytes, false);
            }

            internal ServerConfigState Probe() =>
                VelociraptorServerConfig.ProbeCore(Path, Attributes, Open, Stopwatch.StartNew());
        }

        [TestMethod]
        public void ReadableServerKeyIsOnlyAMarkerAndNeverStoredInFinding()
        {
            var fixture = new Fixture();
            ServerConfigReport report = VelociraptorServerConfig.CollectCore(
                new[] { fixture.Path }, fixture.Attributes, fixture.Open);
            Assert.AreEqual(1, report.Findings.Count);
            Assert.AreEqual(ServerConfigState.ReadableWithCaKey, report.Findings[0].State);
            Assert.AreEqual(1, fixture.OpenCount);
            Assert.IsFalse(report.Partial);
            Assert.IsFalse(report.Findings[0].GetType().GetFields(
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
                .Select(field => Convert.ToString(field.GetValue(report.Findings[0])))
                .Any(value => value != null && value.Contains("private-secret")));
        }

        [TestMethod]
        public void ClientOrUnrelatedPrivateKeyDoesNotProduceCaFinding()
        {
            var fixture = new Fixture();
            fixture.Bytes = Encoding.UTF8.GetBytes("Client:\n  private_key: |\n    -----BEGIN RSA PRIVATE KEY-----\n");
            Assert.AreEqual(ServerConfigState.ReadableWithoutCaKey, fixture.Probe());
            fixture.Bytes = Encoding.UTF8.GetBytes("# CA:\nCA:\n  private_key: ''\nOther:\n  private_key: |\n    -----BEGIN RSA PRIVATE KEY-----\n");
            Assert.AreEqual(ServerConfigState.ReadableWithoutCaKey, fixture.Probe());
            fixture.Bytes = Encoding.UTF8.GetBytes("CA:\n  private_key: |\n    -----BEGIN RSA PRIVATE KEY-----\n");
            Assert.AreEqual(ServerConfigState.ReadableWithoutCaKey, fixture.Probe());
            fixture.Bytes = Encoding.UTF8.GetBytes("CA:\n  private_key: |\n    -----BEGIN RSA PRIVATE KEY-----\n    -----END RSA PRIVATE KEY-----\n");
            Assert.AreEqual(ServerConfigState.ReadableWithCaKey, fixture.Probe());
        }

        [TestMethod]
        public void MissingDeniedLargeAndReparseRemainDistinctAndPassive()
        {
            var fixture = new Fixture { Missing = true };
            Assert.AreEqual(ServerConfigState.Absent, fixture.Probe());
            Assert.AreEqual(0, fixture.OpenCount);

            fixture = new Fixture { Denied = true };
            Assert.AreEqual(ServerConfigState.Denied, fixture.Probe());
            Assert.AreEqual(1, fixture.OpenCount);

            fixture = new Fixture { Bytes = new byte[VelociraptorServerConfig.MaxConfigBytes + 1] };
            Assert.AreEqual(ServerConfigState.TooLarge, fixture.Probe());
            Assert.AreEqual(1, fixture.OpenCount);

            fixture = new Fixture { Reparse = true };
            Assert.AreEqual(ServerConfigState.Unknown, fixture.Probe());
            Assert.AreEqual(0, fixture.OpenCount);
        }

        [TestMethod]
        public void OnlyDirectLocalServerConfigPathsAndSixCandidates()
        {
            Assert.AreEqual(0, VelociraptorServerConfig.CandidatePaths(
                @"\\remote\share", "//remote/share", "").Count());
            var fixture = new Fixture();
            ServerConfigReport report = VelociraptorServerConfig.CollectCore(
                Enumerable.Repeat(fixture.Path, VelociraptorServerConfig.MaxCandidates + 1),
                fixture.Attributes, fixture.Open);
            Assert.AreEqual(VelociraptorServerConfig.MaxCandidates, fixture.OpenCount);
            Assert.IsTrue(report.Partial);
            Assert.AreEqual(ServerConfigState.Unknown, VelociraptorServerConfig.ProbeCore(
                @"\\remote\share\server.config.yaml", fixture.Attributes, fixture.Open, Stopwatch.StartNew()));
        }
    }
}
