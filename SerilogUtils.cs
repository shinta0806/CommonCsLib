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
// ============================================================================

using Serilog;

using Windows.Storage;

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
	/// <param name="generations">�ۑ����鐢��</param>
	/// <param name="path">���O�t�@�C���̃p�X</param>
	public static void CreateLogger(Int32 flleSizeLimit, Int32 generations, String? path = null)
	{
		// �p�X�ݒ�
		if (String.IsNullOrEmpty(path))
		{
			path = DefaultLogPath();
		}

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

	/// <summary>
	/// ���O�ۑ��t�H���_�[�̃f�t�H���g�i���� '\\'�j
	/// </summary>
	/// <returns></returns>
	public static String DefaultLogFolder()
	{
		return Path.GetDirectoryName(ApplicationData.Current.LocalFolder.Path) + "\\" + FOLDER_NAME_LOGS;
	}

	/// <summary>
	/// ���O�ۑ��t�@�C���̃f�t�H���g
	/// </summary>
	/// <returns></returns>
	public static String DefaultLogPath()
	{
		return DefaultLogFolder() + FILE_NAME_LOG;
	}

	// ====================================================================
	// private �萔
	// ====================================================================

	/// <summary>
	/// ���O�t�@�C����
	/// </summary>
	private const String FILE_NAME_LOG = "Log" + Common.FILE_EXT_TXT;

	/// <summary>
	/// ���O�t�H���_�[��
	/// </summary>
	private const String FOLDER_NAME_LOGS = "Logs\\";
}
