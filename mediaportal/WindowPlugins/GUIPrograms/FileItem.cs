#region Copyright (C) 2005-2007 Team MediaPortal

/* 
 *	Copyright (C) 2005-2007 Team MediaPortal
 *	http://www.team-mediaportal.com
 *
 *  This Program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2, or (at your option)
 *  any later version.
 *   
 *  This Program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
 *  GNU General Public License for more details.
 *   
 *  You should have received a copy of the GNU General Public License
 *  along with GNU Make; see the file COPYING.  If not, write to
 *  the Free Software Foundation, 675 Mass Ave, Cambridge, MA 02139, USA. 
 *  http://www.gnu.org/copyleft/gpl.html
 *
 */

#endregion

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using MediaPortal.GUI.Library;
using Programs.Utils;
using SQLite.NET;

namespace ProgramsDatabase
{
  public class FileItem
  {
    #region Variables
    protected static SQLiteClient sqlDB = null;

    int mFileID;
    int mAppID;
    string mTitle;
    string mGameURL;
    string mTitle2;
    string mTitleOptimized;
    string mFilename;
    string mFilepath;
    string mImagefile;
    string mGenre;
    string mGenre2;
    string mGenre3;
    string mGenre4;
    string mGenre5;
    string mCountry;
    string mManufacturer;
    int mYear;
    int mRating;
    string mOverview;
    string mSystem;
    string mManualFilename;
    DateTime mLastTimeLaunched;
    int mLaunchCount;
    int mExtFileID;
    string mTagData;
    string mCategoryData;
    bool mIsFolder;
    List<FileInfo> mFileInfoList = null;
    FileInfo mFileInfoFavourite = null;
    #endregion

    #region Constructors/Destructors
    public FileItem(SQLiteClient initSqlDB)
    {

      // constructor: save SQLiteDB object 
      sqlDB = initSqlDB;
      Clear();
    }
    #endregion

    #region Properties
    public int FileID
    {
      get
      {
        return mFileID;
      }
      set
      {
        mFileID = value;
      }
    }
    public int AppID
    {
      get
      {
        return mAppID;
      }
      set
      {
        mAppID = value;
      }
    }
    public string Title
    {
      get
      {
        return mTitle;
      }
      set
      {
        mTitle = value;
      }
    }
    public string GameURL
    {
      get
      {
        return mGameURL;
      }
      set
      {
        mGameURL = value;
      }
    }
    public string Title2
    {
      get
      {
        return mTitle2;
      }
      set
      {
        mTitle2 = value;
      }
    }
    public string TitleNormalized
    {
      get
      {
        return GetTitleNormalized();
      }
    }
    public string TitleOptimized
    {
      get
      {
        return GetTitleOptimized();
      }
      set
      {
        mTitleOptimized = value;
      }
    }
    public string Filename
    {
      get
      {
        return mFilename;
      }
      set
      {
        mFilename = value;
      }
    }
    public string Filepath
    {
      get
      {
        return mFilepath;
      }
      set
      {
        mFilepath = value;
      }
    }
    public string Imagefile
    {
      get
      {
        return mImagefile;
      }
      set
      {
        mImagefile = value;
      }
    }
    public string Genre
    {
      get
      {
        return mGenre;
      }
      set
      {
        mGenre = value;
      }
    }
    public string Genre2
    {
      get
      {
        return mGenre2;
      }
      set
      {
        mGenre2 = value;
      }
    }
    public string Genre3
    {
      get
      {
        return mGenre3;
      }
      set
      {
        mGenre3 = value;
      }
    }
    public string Genre4
    {
      get
      {
        return mGenre4;
      }
      set
      {
        mGenre4 = value;
      }
    }
    public string Genre5
    {
      get
      {
        return mGenre5;
      }
      set
      {
        mGenre5 = value;
      }
    }
    public string Country
    {
      get
      {
        return mCountry;
      }
      set
      {
        mCountry = value;
      }
    }
    public string Manufacturer
    {
      get
      {
        return mManufacturer;
      }
      set
      {
        mManufacturer = value;
      }
    }
    public int Year
    {
      get
      {
        return mYear;
      }
      set
      {
        mYear = value;
      }
    }
    public int Rating
    {
      get
      {
        return mRating;
      }
      set
      {
        mRating = value;
      }
    }
    public string Overview
    {
      get
      {
        return mOverview;
      }
      set
      {
        mOverview = value;
      }
    }
    public string System_
    {
      get
      {
        return mSystem;
      }
      set
      {
        mSystem = value;
      }
    }
    #endregion

    public virtual void Clear()
    {
      mFileID = -1;
      mAppID = -1;
      mTitle = "";
      mGameURL = "";
      mTitle2 = "";
      mTitleOptimized = "";
      mFilename = "";
      mFilepath = "";
      mImagefile = "";
      mGenre = "";
      mGenre2 = "";
      mGenre3 = "";
      mGenre4 = "";
      mGenre5 = "";
      mCountry = "";
      mManufacturer = "";
      mYear = -1;
      mRating = -1;
      mOverview = "";
      mSystem = "";
      mManualFilename = "";
      mIsFolder = false;
      mLastTimeLaunched = DateTime.MinValue;
      mLaunchCount = 0;
      mExtFileID = -1;
      mTagData = "";
      mCategoryData = "";
    }

    private string GetYearManu()
    {
      string result = "";
      if (mYear <= 0)
      {
        result = mManufacturer;
      }
      else
      {
        result = mManufacturer + " [" + mYear + "]";
      }
      return (result.Trim());
    }

    public string ManualFilename
    {
      get
      {
        return mManualFilename;
      }
      set
      {
        mManualFilename = value;
      }
    }

    public DateTime LastTimeLaunched
    {
      get
      {
        return mLastTimeLaunched;
      }
      set
      {
        mLastTimeLaunched = value;
      }
    }

    public int LaunchCount
    {
      get
      {
        return mLaunchCount;
      }
      set
      {
        mLaunchCount = value;
      }
    }

    public string TagData
    {
      get
      {
        return mTagData;
      }
      set
      {
        mTagData = value;
      }
    }

    public string CategoryData
    {
      get
      {
        return mCategoryData;
      }
      set
      {
        mCategoryData = value;
      }
    }

    public bool IsFolder
    {
      get
      {
        return mIsFolder;
      }
      set
      {
        mIsFolder = value;
      }
    }

    public int ExtFileID
    {
      get
      {
        return mExtFileID;
      }
      set
      {
        mExtFileID = value;
      }
    }

    public string YearManu
    {
      get
      {
        return GetYearManu();
      }
    }

    public List<FileInfo> FileInfoList
    {
      get
      {
        return mFileInfoList;
      }
    }

    public FileInfo FileInfoFavourite
    {
      get
      {
        return mFileInfoFavourite;
      }
      set
      {
        mFileInfoFavourite = value;
      }
    }

    private int CountQuotes(string strVal)
    {
      int at = 0;
      int start = 0;
      int nRes = 0;
      while ((start < strVal.Length) && (at >  - 1))
      {
        at = strVal.IndexOf("\"", start);
        if (at ==  - 1)
          break;
        nRes = nRes + 1;
        start = at + 1;
      }
      return nRes;
    }

    public string ExtractFileName()
    {
      if (Filename == "")
      {
        return "";
      }
      string strRes = "";
      string strSep = "";
      string[] parts = Filename.Split(' ');
      if (Filename.StartsWith("\""))
      {
        // filename is quoted => traverse array and concetenate strings until two quotes are found
        int nNbOfQuotes = 0;
        for (int i = 0; i < parts.Length; i++)
        {
          if (nNbOfQuotes <= 2)
          {
            strRes = strRes + strSep + parts[i];
            strSep = " ";
          }
          if (parts[i].IndexOf("\"") >= 0)
          {
            nNbOfQuotes = nNbOfQuotes + CountQuotes(parts[i]);
            if (nNbOfQuotes == 2)
            {
              break;
            }
          }
        }
      }
      else
      {
        strRes = parts[0];
      }
      return strRes;

    }

    public string ExtractDirectory(string curFilename)
    {
      string strRes = "";
      string strSep = "";
      string[] parts = curFilename.Split('\\');
      for (int i = 0; i < parts.Length - 1; i++)
      {
        strRes = strRes + strSep + parts[i];
        strSep = "\\";
      }
      strRes = strRes.TrimStart('\"');
      return strRes;
    }

    public string ExtractImageExtension()
    {
      string strRes = "";
      string[] parts = this.Imagefile.Split('.');
      if (parts.Length >= 2)
      {
        // there was an extension
        strRes = '.' + parts[parts.Length - 1];
      }
      return strRes;
    }

    public string ExtractImageFileNoPath()
    {
      string strRes = "";
      string[] parts = this.Imagefile.Split('\\');
      if (parts.Length >= 1)
      {
        strRes = parts[parts.Length - 1];
      }
      return strRes;
    }

    public string ExtractArguments()
    {
      string strRes = "";
      string strSep = "";
      string[] parts = Filename.Split(' ');
      if (Filename.StartsWith("\""))
      {
        // filename is quoted => traverse array and concetenate strings after two quotes have been found
        int nNbOfQuotes = 0;
        for (int i = 0; i < parts.Length; i++)
        {
          if (nNbOfQuotes >= 2)
          {
            strRes = strRes + strSep + parts[i];
            strSep = " ";
          }
          if (parts[i].IndexOf("\"") >= 0)
          {
            nNbOfQuotes = nNbOfQuotes + CountQuotes(parts[i]);
          }
        }
      }
      else
      {
        for (int i = 1; i < parts.Length; i++)
        {
          strRes = strRes + strSep + parts[i];
          strSep = " ";
        }
      }
      return strRes;
    }

    private void Insert()
    {
      string strLastLaunch = "";
      string strLaunchCount = "";
      string strYear = "";
      
      strLastLaunch = String.Format("{0}", LastTimeLaunched);
      
      strLaunchCount = String.Format("{0}", LaunchCount); // poor man's inttostr :-)

      if (Year > 0)
      {
        strYear = String.Format("{0}", Year);
      }

      try
      {
        string strSQL = String.Format(
          "insert into tblfile (fileid, " +
          "appid, title, filename, filepath, gameurl, " +
          "imagefile, genre, genre2, genre3, genre4, " +
          "genre5, country, manufacturer, year, rating, " +
          "overview, system, manualfilename, lastTimeLaunched, launchcount, " +
          "isfolder, external_id, uppertitle, tagdata, categorydata" +
          ") values (null, " +
          " '{0}',  '{1}',  '{2}',  '{3}',  '{4}', " +
          " '{5}',  '{6}',  '{7}',  '{8}',  '{9}', " +
          "'{10}', '{11}', '{12}', '{13}', '{14}', " +
          "'{15}', '{16}', '{17}', '{18}', '{19}', " +
          "'{20}', '{21}', '{22}', '{23}', '{24}')",
          AppID, ProgramUtils.Encode(Title), ProgramUtils.Encode(Filename), ProgramUtils.Encode(Filepath), ProgramUtils.Encode(GameURL),
          ProgramUtils.Encode(Imagefile), ProgramUtils.Encode(Genre), ProgramUtils.Encode(Genre2), ProgramUtils.Encode(Genre3), ProgramUtils.Encode(Genre4),
          ProgramUtils.Encode(Genre5), Country, ProgramUtils.Encode(Manufacturer), strYear, Rating,
          ProgramUtils.Encode(Overview), ProgramUtils.Encode(System_), ProgramUtils.Encode(ManualFilename), strLastLaunch, strLaunchCount,
          ProgramUtils.BooleanToStr(IsFolder), ExtFileID, ProgramUtils.Encode(Title.ToUpper()), ProgramUtils.Encode(TagData), ProgramUtils.Encode(CategoryData));
        // Log.Info("dw sql\n{0}", strSQL);
        sqlDB.Execute(strSQL);
      }
      catch (SQLiteException ex)
      {
        Log.Info("programdatabase exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    private void Update()
    {
      string strYear = "";
      if (Year > 0)
      {
        strYear = String.Format("{0}", Year);
      }

      try
      {
        string strSQL = String.Format(
          "update tblfile set " +
          "title = '" + ProgramUtils.Encode(Title) +
          "', filename = '" + ProgramUtils.Encode(Filename) +
          "', filepath = '" + ProgramUtils.Encode(Filepath) +
          "', gameurl = '" + ProgramUtils.Encode(GameURL) +
          "', imagefile = '" + ProgramUtils.Encode(Imagefile) +
          "', genre = '" + ProgramUtils.Encode(Genre) +
          "', genre2 = '" + ProgramUtils.Encode(Genre2) +
          "', genre3 = '" + ProgramUtils.Encode(Genre3) +
          "', genre4 = '" + ProgramUtils.Encode(Genre4) +
          "', genre5 = '" + ProgramUtils.Encode(Genre5) +
          "', country = '" + ProgramUtils.Encode(Country) +
          "', manufacturer = '" + ProgramUtils.Encode(Manufacturer) +
          "', year = '" + strYear +
          "', rating = '" + Rating +
          "', overview = '" + ProgramUtils.Encode(Overview) +
          "', system = '" + ProgramUtils.Encode(System_) +
          "', uppertitle = '" + ProgramUtils.Encode(Title.ToUpper()) +
          "', tagdata = '" + ProgramUtils.Encode(TagData) +
          "', categorydata = '" + ProgramUtils.Encode(CategoryData)) +
          "' where  fileid = " + FileID;

        sqlDB.Execute(strSQL);
      }
      catch (SQLiteException ex)
      {
        Log.Info("programdatabase exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public void UpdateLaunchInfo()
    {
      try
      {
        LastTimeLaunched = DateTime.Now;
        LaunchCount = LaunchCount + 1;
        string strSQL = String.Format("update tblfile set lastTimeLaunched = '{0}', launchcount = {1} where fileid = {2}", LastTimeLaunched, LaunchCount, FileID);
        sqlDB.Execute(strSQL);
      }
      catch (SQLiteException ex)
      {
        Log.Info("programdatabase exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
      }
    }

    public virtual void Write()
    {
      if (mFileID ==  - 1)
      {
        Insert();
      }
      else
      {
        Update();
      }
    }

    public virtual void Delete()
    {
      if (this.FileID >= 0)
      {
        try
        {
          string strSQL1 = String.Format("delete from filteritem where fileid = {0}", this.FileID);
          string strSQL2 = String.Format("delete from tblfile where fileid = {0}", this.FileID);
          sqlDB.Execute(strSQL1);
          sqlDB.Execute(strSQL2);
        }
        catch (SQLiteException ex)
        {
          Log.Info("programdatabase exception err:{0} stack:{1}", ex.Message, ex.StackTrace);
        }
      }
    }

    string GetTitleNormalized()
    {
      return ProgramUtils.NormalizedString(mTitle);
    }

    string GetTitleOptimized()
    {
      if (mTitleOptimized == "")
      {
        return GetTitleNormalized();
      }
      else
      {
        return mTitleOptimized;
      }
    }

    public bool FindFileInfo(myProgScraperType ScraperType)
    {
      int iRetries = 0;
      bool bSuccess = false;
      switch (ScraperType)
      {
        case myProgScraperType.ALLGAME:
          {
            AllGameInfoScraper scraper = new AllGameInfoScraper();
            //					string strTitle = TitleNormalized;
            string strTitle = TitleOptimized;
            while ((!bSuccess) && (iRetries < 5))
            {
              // brute force! Try five times.... sometimes I get
              // a PostHTTP false result... don't know why!
              bSuccess = scraper.FindGameinfo(strTitle);
              if (!bSuccess)
              {
                iRetries++;
              }
            }
            mFileInfoList = scraper.FileInfos;
          }
          break;
      }
      return bSuccess;
    }

    public bool FindFileInfoDetail(AppItem curApp, FileInfo curInfo, myProgScraperType scraperType, ScraperSaveType saveType)
    {
      int iRetries = 0;
      bool bSuccess = false;
      switch (scraperType)
      {
        case myProgScraperType.ALLGAME:
          {
            AllGameInfoScraper scraper = new AllGameInfoScraper();
            while ((!bSuccess) && (iRetries < 5))
            {
              // brute force! Try five times.... sometimes I get
              // a PostHTTP false result... don't know why!
              bSuccess = scraper.FindGameinfoDetail(curApp, this, curInfo, saveType);
              if (!bSuccess)
              {
                iRetries++;
              }
            }
          }
          break;
      }
      return bSuccess;
    }

    public void ToFileInfoFavourite()
    {
      FileInfoFavourite = new FileInfo();

      FileInfoFavourite.Title = this.Title;
      FileInfoFavourite.GameURL = this.mGameURL;
      FileInfoFavourite.Genre = this.Genre;
      FileInfoFavourite.Genre2 = this.Genre2;
      FileInfoFavourite.Genre3 = this.Genre3;
      FileInfoFavourite.Genre4 = this.Genre4;
      FileInfoFavourite.Genre5 = this.Genre5;
      FileInfoFavourite.Manufacturer = this.Manufacturer;
      FileInfoFavourite.Year = this.Year.ToString();
      FileInfoFavourite.Overview = this.Overview;
      FileInfoFavourite.RatingNorm = this.Rating;
      FileInfoFavourite.Platform = this.System_;
    }

    public void SaveFromFileInfoFavourite()
    {
      if (this.FileInfoFavourite == null)
        return;

      if (this.FileInfoFavourite.Loaded == false)
        return;

      // DON'T overwrite title!
      this.Title = FileInfoFavourite.Title;
      this.mGameURL = FileInfoFavourite.GameURL;
      this.Genre = FileInfoFavourite.Genre;
      this.Genre2 = FileInfoFavourite.Genre2;
      this.Genre3 = FileInfoFavourite.Genre3;
      this.Genre4 = FileInfoFavourite.Genre4;
      this.Genre5 = FileInfoFavourite.Genre5;
      this.Manufacturer = FileInfoFavourite.Manufacturer;
      this.Year = ProgramUtils.StrToIntDef(FileInfoFavourite.Year, -1);
      this.Overview = FileInfoFavourite.Overview;
      this.Rating = FileInfoFavourite.RatingNorm;
      this.System_ = FileInfoFavourite.Platform;
      this.Write();
    }

    public string GetNewImageFile(AppItem curApp, string strExtension)
    {
      if (curApp == null)
        return "";
      if (curApp.imageDirs == null)
        return "";
      if (curApp.imageDirs.Length == 0)
        return "";
      if ((this.Imagefile == "") && (this.Filename == ""))
        return "";

      string strFolder = "";
      string strFileName = "";
      string strCand = "";
      int iImgIndex =  - 1;
      bool bFound = false;

      strFolder = curApp.imageDirs[0];

      if (Imagefile != "")
      {
        strFileName = Imagefile.TrimEnd('\"');
        strFileName = strFileName.TrimStart('\"');
        strFileName = Path.GetFileName(strFileName);
      }
      else
      {
        strFileName = Filename.TrimEnd('\"');
        strFileName = strFileName.TrimStart('\"');
        strFileName = Path.GetFileName(strFileName);
      }
      strFileName = Path.ChangeExtension(strFileName, null);

      while (!bFound)
      {
        iImgIndex++;
        if (iImgIndex == 0)
        {
          strCand = String.Format("{0}\\{1}{2}", strFolder, strFileName, strExtension);
        }
        else
        {
          strCand = String.Format("{0}\\{1}_{2}{3}", strFolder, strFileName, iImgIndex, strExtension);
        }
        bFound = !File.Exists(strCand);
      }
      return strCand;
    }

    public void DeleteImages(AppItem curApp)
    {
      if (curApp == null)
        return ;
      if (curApp.imageDirs == null)
        return ;
      if (curApp.imageDirs.Length == 0)
        return ;
      string strFolder = "";
      string strFileName = "";

      strFolder = curApp.imageDirs[0];
      strFileName = Filename.TrimEnd('\"');
      strFileName = strFileName.TrimStart('\"');
      strFileName = Path.GetFileName(strFileName);
      strFileName = Path.ChangeExtension(strFileName, null);
      strFileName = strFileName + "*.jpg"; // todo: also treat other pic extensions!


      if (Directory.Exists(strFolder))
      {
        string[] files = Directory.GetFiles(strFolder, strFileName);
        foreach (string file2Delete in files)
        {
          File.Delete(file2Delete);
        }
      }
    }

    public string GetValueOfTag(string TagName)
    {
      string strLowerTag = TagName.ToLower() + "=";
      string strLowerItem = "";
      string result = "";
      foreach (string strItem in this.mTagData.Split('\r'))
      {
        strLowerItem = strItem.ToLower();
        if (strLowerItem.StartsWith(strLowerTag))
        {
          result = strItem.Remove(0, strLowerTag.Length);
        }
        else if (strLowerItem.StartsWith("\n" + strLowerTag))
        {
          result = strItem.Remove(0, strLowerTag.Length + 1);
        }
      }
      result = result.Replace("\n", "");
      result = result.Replace("\r", "");
      result = result.TrimStart('\"');
      result = result.TrimEnd('\"');
      return result;
    }

    public string GetValueOfCategory(int CatIndex)
    {
      string result = "";
      ArrayList Categories = new ArrayList(mCategoryData.Split('\r'));
      if ((CatIndex >= 0) && (CatIndex <= Categories.Count - 1))
      {
        string strLine = (string)Categories[CatIndex];
        strLine = strLine.TrimStart('\r');
        bool bValueLine = false;
        bool bFirst = true;
        string strSep = "";
        foreach (string strToken in strLine.Split('='))
        {
          //Log.Info("getvalueofcategory dw token {0}", strToken);
          if (!bFirst)
          {
            bValueLine = true;
            //						bValueLine = strToken.EndsWith("\"");
          }
          if (bValueLine)
          {
            result = result + strSep + strToken;
            strSep = "=";
          }
          bFirst = false;
        }
      }
      result = result.Replace("\n", "");
      result = result.Replace("\r", "");
      result = result.TrimStart('\"');
      result = result.TrimEnd('\"');
      return result;
    }

    public string GetNameOfCategory(int CatIndex)
    {
      string result = "";
      ArrayList Categories = new ArrayList(mCategoryData.Split('\r'));
      if ((CatIndex >= 0) && (CatIndex <= Categories.Count - 1))
      {
        string strLine = (string)Categories[CatIndex];
        strLine = strLine.TrimStart('\r');
        bool bValueLine = false;
        bool bFirst = true;
        string strSep = "";
        foreach (string strToken in strLine.Split('='))
        {
          strToken.Replace("\n", "");
          strToken.Replace("\r", "");
          //Log.Info("getnameofcategory dw token {0}", strToken);
          if (!bFirst)
          {
            //doesn't work			bValueLine = strToken.EndsWith("\"");
            bValueLine = true;
          }
          if (!bValueLine)
          {
            result = result + strSep + strToken;
            strSep = "=";
          }
          else
          {
            break;
          }
          bFirst = false;
        }
      }
      result = result.Replace("\n", "");
      result = result.Replace("\r", "");
      result = result.TrimStart('\"');
      result = result.TrimEnd('\"');
      return result;
    }
  }
}
