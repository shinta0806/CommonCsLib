// ============================================================================
// 
// Serilog ���p�֐�
// Copyright (C) 2022 by SHINTA
// 
// ============================================================================

// ----------------------------------------------------------------------------
// �ȉ��̃p�b�P�[�W���C���X�g�[������Ă���O��
//   Serilog.Sinks.File
//   Serilog.Sinks.Debug
//   Serilog.Enrichers.Process
//   Serilog.Enrichers.Thread
// ----------------------------------------------------------------------------

// ============================================================================
//  Ver.  |      �X�V��      |                    �X�V���e
// ----------------------------------------------------------------------------
//  -.--  | 2022/11/03 (Thu) | �쐬�J�n�B
//  1.00  | 2022/11/03 (Thu) | �t�@�[�X�g�o�[�W�����B
// (1.01) | 2022/11/05 (Sat) |   CreateLogger() �� path ��K�{�ɂ����B
// ============================================================================

using Serilog;

namespace Shinta;

internal class SerilogUtils
{
	// ====================================================================
	// public �֐�
	// ====================================================================

	/// <summary>
	/// ���K�[����
	/// </summary>
	/// <param name="flleSizeLimit">1 �̃��O�t�@�C���̏���T�C�Y [Bytes]</param>
	/// <param name="generations">�ۑ����鐢��i���s������܂ށj</param>
	/// <param name="path">���O�t�@�C���̃p�X</param>
	public static void CreateLogger(Int32 flleSizeLimit, Int32 generations, String path)
	{
		Log.Logger = new LoggerConfiguration()
				.MinimumLevel.Information()
#if DEBUG
				.MinimumLevel.Debug()
				.WriteTo.Debug()
#endif
				.Enrich.WithProcessId()
				.Enrich.WithThreadId()
				.WriteTo.File(path, rollOnFileSizeLimit: true, fileSizeLimitBytes: flleSizeLimit, retainedFileCountLimit: generations,
				outputTemplate: "{Timestamp:yyyy/MM/dd HH:mm:ss.fff}\t{ProcessId}/M{ThreadId}\t{Level:u3}\t{Message:lj}{NewLine}{Exception}")
				.CreateLogger();
	}
}
